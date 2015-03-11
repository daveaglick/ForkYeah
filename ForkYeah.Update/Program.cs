using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForkYeah.Data;
using Octokit;

namespace ForkYeah.Update
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            int updated = 0;

            try
            {
                using(ForkYeahContext context = new ForkYeahContext())
                {
                    // Get groups of 100 (see http://stackoverflow.com/a/419063/807064)
                    List<List<Data.Repository>> repositories = context.Repositories
                        .ToList()
                        .Select((x, i) => new { Respository = x, Index = i })
                        .GroupBy(x => x.Index / 100)
                        .Select(x => x.Select(v => v.Respository).ToList())
                        .ToList();

                    // Record update start (with total count)
                    UpdateHistory history = new UpdateHistory()
                    {
                        StartTime = startTime,
                        TotalCount = repositories.Sum(x => x.Count)
                    };
                    context.UpdateHistories.Add(history);
                    context.SaveChanges();

                    // Get the GitHub client
                    string token = ConfigurationManager.AppSettings["GitHubToken"];
                    GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                    github.Credentials = new Credentials(token);

                    // Iterate and update
                    foreach(List<Data.Repository> batch in repositories)
                    {
                        // Query GitHub
                        int batchUpdated = 0;
                        SearchRepositoriesRequest searchRequest = new SearchRepositoriesRequest(
                            string.Join(" ", batch.Select(x => string.Format("repo:{0}/{1}", x.Owner, x.Name))));
                        SearchRepositoryResult searchResult = AsyncHelper.RunSync(() => github.Search.SearchRepo(searchRequest));
                        foreach(Octokit.Repository repo in searchResult.Items)
                        {
                            Data.Repository dataRepo = batch.SingleOrDefault(x => x.Owner == repo.Owner.Login && x.Name == repo.Name);
                            if(dataRepo != null)
                            {
                                // Update the repository data
                                dataRepo.Description = repo.Description;
                                dataRepo.OwnerHtmlUrl = repo.Owner.HtmlUrl;
                                dataRepo.HtmlUrl = repo.HtmlUrl;
                                dataRepo.Homepage = repo.Homepage;
                                dataRepo.Language = repo.Language;
                                dataRepo.StargazersCount = repo.StargazersCount;
                                if (dataRepo.DbAdded >= startTime.AddHours(-96))
                                {
                                    // Only update count change if this is an active listing
                                    dataRepo.StargazersCountChange = repo.StargazersCount - dataRepo.OriginialStargazersCount;
                                }
                                dataRepo.ForksCount = repo.ForksCount;
                                dataRepo.OpenIssuesCount = repo.OpenIssuesCount;
                                dataRepo.CreatedAt = repo.CreatedAt;
                                dataRepo.UpdatedAt = repo.UpdatedAt;
                                dataRepo.PushedAt = repo.PushedAt;
                                dataRepo.DbUpdated = startTime;

                                batchUpdated++;
                            }
                            else
                            {
                                Console.WriteLine("GitHub returned unexpected repository: {0}/{1}", repo.Owner.Login, repo.Name);
                            }
                        }

                        // Check for missed repositories
                        foreach(Data.Repository dataRepo in batch.Where(x => x.DbUpdated < startTime))
                        {
                            Console.WriteLine("Did not get search result for repository: {0}/{1}", dataRepo.Owner, dataRepo.Name);
                        }

                        // Save updates
                        context.SaveChanges();
                        updated += batchUpdated;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                using (ForkYeahContext context = new ForkYeahContext())
                {
                    UpdateHistory history = context.UpdateHistories.SingleOrDefault(x => x.StartTime == startTime);
                    if (history != null)
                    {
                        history.Exception = ex.ToString();
                        history.EndTime = DateTime.UtcNow;
                        history.UpdatedCount = updated;
                        context.SaveChanges();
                    }
                }
                Console.WriteLine("Exception: " + ex);
                throw;
            }

            // Done!
            using (ForkYeahContext context = new ForkYeahContext())
            {
                UpdateHistory history = context.UpdateHistories.Single(x => x.StartTime == startTime);
                history.EndTime = DateTime.UtcNow;
                history.UpdatedCount = updated;
                context.SaveChanges();
            }
            Console.WriteLine("Done!");
        }
    }
}
