using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ForkYeah.Data;
using ForkYeah.Models.Default;

namespace ForkYeah.Controllers
{
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
        
        [Route("ranked")]
        public virtual ActionResult Ranked()
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

            return PartialView(MVC.Default.Views.ViewNames.Repositories, repositories);
        }
        
        [Route("{owner}/{name}")]
        public virtual ActionResult Details(string owner, string name)
        {
            return PartialView();
        }
    }
}