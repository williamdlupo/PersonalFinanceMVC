using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using PersonalFinance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace PersonalFinance.Controllers
{
    [Authorize]
    public class OnboardingController : Controller
    {
        //
        // GET: /Onboarding/Goals
        public ActionResult Goals()
        {
            return View();
        }

        //
        //GET: /Onboarding/LoadAccounts
        public ActionResult LoadAccounts()
        {
            return View();
        }
    }
}