using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ForkYeah.Data;
using ForkYeah.Models.Default;
using Octokit;

namespace ForkYeah.Controllers
{
    [NoCache]
    public partial class DefaultController : Controller
    {
        private ForkYeahContext _db = new ForkYeahContext();

        // This should be the only GET route - all the SPA views are handled via POST
        [Route("{*path}")]
        [HttpGet]
        public virtual ActionResult Index(string path)
        {
            Index model = new Index();

            // Get the username (okay to do this here and wait for it since index will only be called once)
            model.Authorized = Request.Cookies.AllKeys.Contains("GitHubToken");
            if (model.Authorized)
            {
                try
                {
                    string protectedTokenString = Request.Cookies["GitHubToken"].Value;
                    byte[] protectedTokenBytes = System.Text.Encoding.ASCII.GetBytes(protectedTokenString);
                    byte[] tokenBytes = MachineKey.Unprotect(protectedTokenBytes);
                    string token = System.Text.Encoding.ASCII.GetString(tokenBytes);

                    GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                    github.Credentials = new Credentials(token);
                    User user = AsyncHelper.RunSync(() => github.User.Current());
                    model.UserLogin = user.Login;
                    model.UserAvatarUrl = user.AvatarUrl;
                    model.UserHtmlUrl = user.HtmlUrl;
                }
                catch (Exception)
                {
                    model.Authorized = false;
                    Request.Cookies.Remove("GitHubToken");
                }
            }

            // Get the languages
            List<KeyValuePair<string, string>> languages = _db.Repositories
                .Select(x => x.Language)
                .Distinct()
                .OrderBy(x => x)
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new KeyValuePair<string, string>(x, x))
                .ToList();
            languages.Insert(0, new KeyValuePair<string,string>("All Languages", string.Empty));
            model.Languages = languages;

            return View(model);
        }

        [Route("auth")]
        [HttpGet]
        public virtual ActionResult Auth()
        {
            // Get the client info
            string clientId = System.Configuration.ConfigurationManager.AppSettings["GitHubClientId"];

            // Store a random string to prevent Cross-Site Request Forgery
            string csrf = Membership.GeneratePassword(24, 1);
            Session["CSRF:State"] = csrf;

            // Get the login request URL
            GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
            OauthLoginRequest loginRequest = new OauthLoginRequest(clientId)
            {
                Scopes = { "user" },
                State = csrf
            };
            return Redirect(github.Oauth.GetGitHubLoginUrl(loginRequest).ToString());
        }

        [Route("auth-complete")]
        [HttpGet]
        public virtual ActionResult AuthComplete(string code, string state)
        {
            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    // Get the client info
                    string clientId = System.Configuration.ConfigurationManager.AppSettings["GitHubClientId"];
                    string clientSecret = System.Configuration.ConfigurationManager.AppSettings["GitHubClientSecret"];

                    // Check the Cross-Site Request Forgery string
                    string csrf = Session["CSRF:State"] as string;
                    if (csrf == state)
                    {
                        // Get and store the token
                        GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                        OauthTokenRequest tokenRequest = new OauthTokenRequest(clientId, clientSecret, code);
                        OauthToken token = AsyncHelper.RunSync(() => github.Oauth.CreateAccessToken(tokenRequest));
                        byte[] tokenBytes = System.Text.Encoding.ASCII.GetBytes(token.AccessToken);
                        byte[] protectedToken = MachineKey.Protect(tokenBytes);
                        string protectedTokenString = System.Text.Encoding.ASCII.GetString(protectedToken);
                        Request.Cookies.Add(new HttpCookie("GitHubToken", protectedTokenString));
                    }
                }
            }
            catch (Exception)
            {
            }
            Session["CSRF:State"] = null;
            return RedirectToAction(MVC.Default.Index());
        }

        [Route("")]
        [HttpPost]
        public virtual ActionResult Intro()
        {
            // TODO: Display login page if not logged in, other content (such as add) if logged in
            return PartialView("Add");
        }

        [Route("add")]
        [HttpPost]
        public virtual ActionResult Add()
        {
            return PartialView();
        }

        [Route("add-submit")]
        [HttpPost]
        public virtual ActionResult AddSubmit(string owner, string name)
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

        [Route("active")]
        [HttpPost]
        public virtual ActionResult Active()
        {
            string language = GetLanguage();
            DateTimeOffset activeOffset = DateTimeOffset.Now.AddHours(-96);
            IEnumerable<RepositoryListItem> repositories = _db.Repositories
                .Where(x => x.DbAdded >= activeOffset)
                .Where(x => language == null || x.Language == language)
                .OrderByDescending(x => x.StargazersCountChange)
                .Select(x => new RepositoryListItem
                {
                    DbAdded = x.DbAdded,
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

        [Route("archive")]
        [HttpPost]
        public virtual ActionResult Archive(int page = 0)
        {
            string language = GetLanguage();
            int pageSize = 20;
            DateTimeOffset activeOffset = DateTimeOffset.Now.AddHours(-96);
            IEnumerable<RepositoryListItem> repositories = _db.Repositories
                .Where(x => x.DbAdded < activeOffset)
                .Where(x => language == null || x.Language == language)
                .OrderByDescending(x => x.DbAdded)
                .Select(x => new RepositoryListItem
                {
                    DbAdded = x.DbAdded,
                    Owner = x.Owner,
                    Name = x.Name,
                    Description = x.Description,
                    Language = x.Language,
                    HtmlUrl = x.HtmlUrl,
                    StargazersCount = x.StargazersCount,
                    StargazersCountChange = x.StargazersCountChange
                })
                .Skip(page * pageSize)
                .Take(pageSize);

            return PartialView(new Archive()
            {
                ListItems = repositories,
                NewerPage = page != 0 ? page - 1 : (int?)null,
                OlderPage = repositories.Count() == pageSize ? page + 1 : (int?)null
            });
        }

        private string GetLanguage()
        {
            string language = null;
            if (Request.Cookies.AllKeys.Contains("language"))
            {
                language = HttpUtility.UrlDecode(Request.Cookies["language"].Value);
            }
            return string.IsNullOrWhiteSpace(language) ? null : language;
        }

        [Route("{owner}/{name}")]
        [HttpPost]
        public virtual ActionResult Details(string owner, string name)
        {
            Data.Repository repository = _db.Repositories.FirstOrDefault(x => x.Owner == owner && x.Name == name);
            if (repository == null)
            {
                return HttpNotFound();
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