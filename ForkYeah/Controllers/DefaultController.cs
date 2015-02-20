using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ForkYeah.Controllers
{
    public partial class DefaultController : Controller
    {
        [Route("")]
        public virtual ActionResult Index()
        {
            return View();
        }
    }
}