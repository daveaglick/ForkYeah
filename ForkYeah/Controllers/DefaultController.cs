using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ForkYeah.Data;
using ForkYeah.Models.Default;

namespace ForkYeah.Controllers
{
    public partial class DefaultController : AsyncController
    {
        private ForkYeahContext _db = new ForkYeahContext();

        [Route("")]
        public virtual async Task<ActionResult> Index()
        {
            return View();
        }
        
        [Route("add")]
        public virtual async Task<ActionResult> Add()
        {
            return PartialView();
        }

        [HttpPost]
        [Route("add")]
        public virtual async Task<ActionResult> Add(string owner, string name)
        {
            if(string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name))
            {
                return Content("A repository owner and name must be provided.");
            }
            return Content(string.Empty);
        }
        
        [Route("list")]
        public virtual async Task<ActionResult> List()
        {
            List<RepositoryListItem> repositories = new List<RepositoryListItem>();
            
            // Dummy data
            for (int c = 0; c < 100; c++ )
            {
                repositories.Add(new Models.Default.RepositoryListItem
                {
                    Owner = "somedave",
                    Name = "FluentBootstrap",
                    Description = "Provides extensions, helper classes, model binding, and other goodies to help you use the Bootstrap CSS framework from .NET code.",
                    Language = "C#",
                    HtmlUrl = "https://github.com/somedave/FluentBootstrap",
                    StargazersCount = 24,
                    StargazersIncrease = 5
                });
            }

            return PartialView(repositories);
        }
        
        [Route("{owner}/{name}")]
        public virtual async Task<ActionResult> Details(string owner, string name)
        {
            return PartialView();
        }
    }
}