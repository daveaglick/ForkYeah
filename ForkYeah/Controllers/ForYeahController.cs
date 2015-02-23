using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ForkYeah.Data;

namespace ForkYeah.Controllers
{
    public partial class ForkYeahController : Controller
    {
        private ForkYeahContext _db = new ForkYeahContext();

        [Route("")]
        public virtual ActionResult Index()
        {
            return View();
        }
    }
}