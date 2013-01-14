using System;
using System.Web.Mvc;

namespace BasicScenarioWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {            
            return View();
        }
    }
}
