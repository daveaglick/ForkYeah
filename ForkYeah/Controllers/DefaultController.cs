using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ForkYeah.Data;
using ForkYeah.Models.Default;
using Octokit;

namespace ForkYeah.Controllers
{
    [NoCache]
    public partial class DefaultController : Controller
    {
        private ForkYeahContext _db = new ForkYeahContext();

        [Route("")]
        public virtual ActionResult Index()
        {
            return View();
        }

        [Route("add")]
        public virtual ActionResult Add()
        {
            return PartialView();
        }

        [HttpPost]
        [Route("add")]
        public virtual ActionResult Add(string owner, string name)
        {
            // Null and whitespace checks
            if(string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name))
            {
                return Content("A repository owner and name must be provided.");
            }
            
            // Communicate with GitHub
            Octokit.Repository repository;
            int commitCount;
            int contributorCount;
            string readmeHtml;
            try
            {
                GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                
                // Fetch the repository
                try
                {
                    repository = AsyncHelper.RunSync(() => github.Repository.Get(owner, name));
                }
                catch (NotFoundException)
                {
                    return Content("That repository was not found, please double-check it.");
                }

                // Make sure it's not a fork
                if (repository.Fork)
                {
                    return Content(string.Format("That repository is a fork of {0}. Submissions are limited to top-level repositories.", repository.Parent.FullName));
                }

                // Check number of stars
                if (repository.StargazersCount > 100)
                {
                    return Content(string.Format("That repository has {0} stars, which is too many. Limiting submissions to projects with fewer than 100 stars helps ensure attention for those projects that remain undiscovered.", repository.StargazersCount));
                }

                // Get the contributors
                IEnumerable<Contributor> contributors = AsyncHelper.RunSync(() => github.Repository.Statistics.GetContributors(owner, name));
                contributorCount = contributors.Count();

                // Make sure there are enough commits
                commitCount = contributors.Sum(x => x.Total);
                if(commitCount < 20)
                {
                    return Content(string.Format("That repository has {0} commits, which is not enough. Limiting submissions to projects with at least 20 commits helps ensure that the project is actively maintained.", commitCount));
                }

                // Make sure that there's enough history
                DateTimeOffset earliestCommit = contributors.Min(x => x.Weeks.Where(w => w.Commits > 0).Select(w => w.Week).Min());
                if (earliestCommit > DateTimeOffset.Now.AddMonths(-3))
                {
                    return Content(string.Format("The earliest commit for that repository was {0}, which is not old enough. Limiting submissions to projects with at least 3 months of history helps ensure that the project is actively maintained.", earliestCommit.ToString("d")));
                }

                // Make sure there's a readme
                try
                {
                    readmeHtml = AsyncHelper.RunSync(() => github.Repository.Content.GetReadmeHtml(owner, name));
                }
                catch(NotFoundException)
                {
                    return Content("That repository does not have a readme file. Limiting submissions to projects with readme files helps make sure the project is clear and understandable.");
                }
            }
            catch(Exception)
            {
                return Content("Something unexpected happened when communicating with GitHub, please try again.");
            }    
  
            // Make sure it's not already in the database
            Data.Repository entity = _db.Repositories.FirstOrDefault(x => x.Owner == repository.Owner.Login && x.Name == repository.Name);
            if(entity != null)
            {
                return Content(string.Format("That repository was already added on {0}.", entity.DbAdded.ToString("d")));
            }
      
            // All the requirements have been met, go ahead and add the project
            entity = new Data.Repository()
            {
                Owner = repository.Owner.Login,
                Name = repository.Name,
                DbAdded = DateTimeOffset.Now,
                OriginialStargazersCount = repository.StargazersCount,
                Description = repository.Description,
                OwnerHtmlUrl = repository.Owner.HtmlUrl,
                HtmlUrl = repository.HtmlUrl,
                Homepage = repository.Homepage,
                Language = repository.Language,
                StargazersCount = repository.StargazersCount,
                StargazersCountChange = 0,
                ForksCount = repository.ForksCount,
                OpenIssuesCount = repository.OpenIssuesCount,
                CreatedAt = repository.CreatedAt,
                UpdatedAt = repository.UpdatedAt,
                PushedAt = repository.PushedAt,
                DbUpdated = DateTimeOffset.Now,
                ReadmeHtml = readmeHtml,
                ContributorCount = contributorCount,
                CommitCount = commitCount,
                DbDetailsUpdated = DateTimeOffset.Now
            };
            _db.Repositories.Add(entity);
            _db.SaveChanges();

            return Content(string.Empty);
        }
        
        [Route("list")]
        public virtual ActionResult List()
        {
            DateTimeOffset twoDaysAgo = DateTimeOffset.Now.AddHours(-48);
            IEnumerable<RepositoryListItem> repositories = _db.Repositories
                .Where(x => x.DbAdded >= twoDaysAgo)
                .OrderByDescending(x => x.StargazersCountChange)
                .Select(x => new RepositoryListItem
                {
                    Owner = x.Owner,
                    Name = x.Name,
                    Description = x.Description,
                    Language = x.Language,
                    HtmlUrl = x.HtmlUrl,
                    StargazersCount = x.StargazersCount,
                    StargazersCountChange = x.StargazersCountChange
                });
            return PartialView(repositories);
        }

        [Route("{owner}/{name}")]
        public virtual ActionResult Details(string owner, string name)
        {
            Data.Repository repository = _db.Repositories.FirstOrDefault(x => x.Owner == owner && x.Name == name);
            if (repository == null)
            {
                // TODO: How to return an error page? Can we return HttpNotFound?
            }

            return PartialView(new RepositoryDetails()
            {
                Owner = repository.Owner,
                Name = repository.Name,
                DbAdded = repository.DbAdded,
                Description = repository.Description,
                OwnerHtmlUrl = repository.OwnerHtmlUrl,
                HtmlUrl = repository.HtmlUrl,
                Homepage = repository.Homepage,
                Language = repository.Language,
                StargazersCount = repository.StargazersCount,
                StargazersCountChange = repository.StargazersCountChange,
                ForksCount = repository.ForksCount,
                OpenIssuesCount = repository.OpenIssuesCount,
                ReadmeHtml = repository.ReadmeHtml,
                ContributorCount = repository.ContributorCount,
                CommitCount = repository.CommitCount
            });
        }
    }
}