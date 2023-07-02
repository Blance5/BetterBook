using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalBookProj.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string error)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("LoggedIn", "Books", new { error });
            }

            ViewBag.Error = error;
            return View();
        }
    }
}