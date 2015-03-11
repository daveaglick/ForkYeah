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

            try
            {
                string token = GetTokenCookie();
                if (token != null)
                {
                    // Get the username (okay to do this here and wait for it since index will only be called once)
                    model.Authorized = true;
                    GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                    github.Credentials = new Credentials(token);
                    User user = AsyncHelper.RunSync(() => github.User.Current());
                    model.UserLogin = user.Login;
                    model.UserAvatarUrl = user.AvatarUrl;
                    model.UserHtmlUrl = user.HtmlUrl;

                    // Also get the user's starred repositories
                    IEnumerable<Octokit.Repository> starred = AsyncHelper.RunSync(() => github.Activity.Starring.GetAllForCurrent());
                    Session["Starred"] = starred.Select(x => x.Owner.Login + " " + x.Name).ToList();
                }
            }
            catch (Exception)
            {
                model.Authorized = false;
                SetTokenCookie(null);
                Session.Remove("Starred");
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

        private void SetTokenCookie(string token)
        {
            if(token == null)
            {
                // Clear the token cookie
                Response.Cookies.Add(new HttpCookie("GitHubToken")
                {
                    Expires = DateTime.Now.AddDays(-1)
                });
                return;
            }
            string tokenPassword = System.Configuration.ConfigurationManager.AppSettings["GitHubTokenPassword"];
            string tokenSalt = System.Configuration.ConfigurationManager.AppSettings["GitHubTokenSalt"];
            string encryptedToken = EncryptionHelper.Encrypt(token, tokenPassword, tokenSalt);
            string encodedToken = HttpUtility.HtmlEncode(encryptedToken);
            Response.Cookies.Add(new HttpCookie("GitHubToken", Uri.EscapeDataString(encodedToken))
            {
                Expires = DateTime.Now.AddYears(1)
            });
        }

        private string GetTokenCookie()
        {
            string token = null;
            if (Request.Cookies.AllKeys.Contains("GitHubToken"))
            {
                string tokenPassword = System.Configuration.ConfigurationManager.AppSettings["GitHubTokenPassword"];
                string tokenSalt = System.Configuration.ConfigurationManager.AppSettings["GitHubTokenSalt"];
                string encodedToken = Uri.UnescapeDataString(Request.Cookies["GitHubToken"].Value);
                string encryptedToken = HttpUtility.HtmlDecode(encodedToken);
                token = EncryptionHelper.Decrypt(encryptedToken, tokenPassword, tokenSalt);
            }
            return token;
        }

        [Route("auth")]
        [HttpGet]
        public virtual ActionResult Auth(string path, string owner, string name)
        {
            // Get the client info
            string clientId = System.Configuration.ConfigurationManager.AppSettings["GitHubClientId"];

            // Store a random string to prevent Cross-Site Request Forgery
            string csrf = Membership.GeneratePassword(24, 1);
            Session["Csrf"] = csrf;

            // Get the login request URL
            GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
            OauthLoginRequest loginRequest = new OauthLoginRequest(clientId)
            {
                Scopes = { "public_repo" },
                State = csrf + " " + path + " " + owner + " " + name
            };
            return Redirect(github.Oauth.GetGitHubLoginUrl(loginRequest).ToString());
        }

        [Route("auth-complete")]
        [HttpGet]
        public virtual ActionResult AuthComplete(string code, string state)
        {
            string path = null;
            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    // Get the client info
                    string clientId = System.Configuration.ConfigurationManager.AppSettings["GitHubClientId"];
                    string clientSecret = System.Configuration.ConfigurationManager.AppSettings["GitHubClientSecret"];

                    // Get the state
                    string[] stateSplit = state.Split(new [] { ' ' }, StringSplitOptions.None);
                    string stateCsrf = stateSplit[0];
                    if (stateSplit.Length > 1 && !string.IsNullOrWhiteSpace(stateSplit[1]))
                    {
                        path = stateSplit[1];
                    }
                    string owner = stateSplit.Length > 2 ? stateSplit[2] : null;
                    string name = stateSplit.Length > 3 ? stateSplit[3] : null;

                    // Check the Cross-Site Request Forgery string
                    string csrf = Session["Csrf"] as string;
                    if (csrf == stateCsrf)
                    {
                        // Get and store the token
                        GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                        OauthTokenRequest tokenRequest = new OauthTokenRequest(clientId, clientSecret, code);
                        OauthToken token = AsyncHelper.RunSync(() => github.Oauth.CreateAccessToken(tokenRequest));
                        SetTokenCookie(token.AccessToken);

                        // Check if we're supposed to star something
                        if(!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(name))
                        {
                            Star(owner, name);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            Session["Csrf"] = null;
            return Redirect(string.IsNullOrWhiteSpace(path) ? "/" : path);
        }

        [Route("logout")]
        [HttpGet]
        public virtual ActionResult Logout()
        {
            SetTokenCookie(null);
            Session.Remove("Starred");
            return RedirectToAction(Index());
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
            owner = owner.Trim();
            name = name.Trim();
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
                if (repository.StargazersCount > 200)
                {
                    return Content(string.Format("That repository has {0} stars, which is too many. Limiting submissions to projects with fewer than 200 stars helps ensure attention for those projects that remain undiscovered.", repository.StargazersCount));
                }

                // Get the contributors
                IEnumerable<Contributor> contributors = AsyncHelper.RunSync(() => github.Repository.Statistics.GetContributors(owner, name));
                contributorCount = contributors.Count();

                // Make sure there are enough commits
                commitCount = contributors.Sum(x => x.Total);
                if(commitCount < 10)
                {
                    return Content(string.Format("That repository has {0} commits, which is not enough. Limiting submissions to projects with at least 10 commits helps ensure that the project is actively maintained.", commitCount));
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
            List<string> starred = Session["Starred"] as List<string>;
            string language = GetLanguage();
            DateTimeOffset activeOffset = DateTimeOffset.Now.AddHours(-96);
            IEnumerable<RepositoryListItem> repositories = _db.Repositories
                .Where(x => x.DbAdded >= activeOffset)
                .Where(x => language == null || x.Language == language)
                .OrderByDescending(x => x.StargazersCountChange)
                .ToList()
                .Select(x => new RepositoryListItem
                {
                    DbAdded = x.DbAdded,
                    Owner = x.Owner,
                    Name = x.Name,
                    Description = x.Description,
                    Language = x.Language,
                    HtmlUrl = x.HtmlUrl,
                    StargazersCount = x.StargazersCount,
                    StargazersCountChange = x.StargazersCountChange,
                    Starred = starred != null && starred.Contains(x.Owner + " " + x.Name)
                });

            return PartialView(repositories);
        }

        [Route("archive")]
        [HttpPost]
        public virtual ActionResult Archive(int page = 0)
        {
            List<string> starred = Session["Starred"] as List<string>;
            string language = GetLanguage();
            int pageSize = 20;
            DateTimeOffset activeOffset = DateTimeOffset.Now.AddHours(-96);
            IEnumerable<RepositoryListItem> repositories = _db.Repositories
                .Where(x => x.DbAdded < activeOffset)
                .Where(x => language == null || x.Language == language)
                .OrderByDescending(x => x.DbAdded)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(x => new RepositoryListItem
                {
                    DbAdded = x.DbAdded,
                    Owner = x.Owner,
                    Name = x.Name,
                    Description = x.Description,
                    Language = x.Language,
                    HtmlUrl = x.HtmlUrl,
                    StargazersCount = x.StargazersCount,
                    StargazersCountChange = x.StargazersCountChange,
                    Starred = starred != null && starred.Contains(x.Owner + " " + x.Name)
                });

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
                language = Uri.UnescapeDataString(Request.Cookies["language"].Value);
            }
            return string.IsNullOrWhiteSpace(language) ? null : language;
        }

        [Route("{owner}/{name}")]
        [HttpPost]
        public virtual ActionResult Details(string owner, string name)
        {
            // Get the repository domain model
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

        [Route("{owner}/{name}/Star")]
        [HttpPost]
        public virtual ActionResult Star(string owner, string name)
        {
            // Get the repository domain model
            Data.Repository repository = _db.Repositories.FirstOrDefault(x => x.Owner == owner && x.Name == name);
            if (repository == null)
            {
                return HttpNotFound();
            }

            // Attempt to add a star
            try
            {
                string token = GetTokenCookie();
                if (token != null)
                {
                    GitHubClient github = new GitHubClient(new ProductHeaderValue("ForkYeah"));
                    github.Credentials = new Credentials(token);
                    AsyncHelper.RunSync(() => github.Activity.Starring.StarRepo(owner, name));
                }
                else
                {
                    return Content("auth");
                }
            }
            catch(AuthorizationException)
            {
                // A return value indicates to the JS handler to redirect to /auth
                return Content("auth");
            }

            // Note that we don't update the database here - that would introduce a way to game the system by repeatedly 
            // starring on FY then unstarring on GH - the count will get updated in the DB on the next background refresh

            // Might not have the session object if starring caused the auth process
            List<string> starred = Session["Starred"] as List<string>;
            if (starred != null)
            {
                starred.Add(owner + " " + name);
                Session["Starred"] = starred;
            }

            return Content(string.Empty);
        }
    }
}