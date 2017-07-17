using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using PersonalFinance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PersonalFinance.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {

        private ApplicationDbContext ApplicationDbContext { get; set; }
        private UserManager<ApplicationUser> UserManager { get; set; }
        private ApplicationUser user;

        //Constructor- creates user object the current user that is logged in
        public DashboardController()
        {
            this.ApplicationDbContext = new ApplicationDbContext();
            this.UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(ApplicationDbContext));
            user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
        }

        //
        // GET: Dashboard/Main
        public ActionResult Main()
        {
            //move to goal selection controller when post methods created
            //user.FirstLoginFlag = false;
            //await UserManager.UpdateAsync(user);

            if (user.FirstLoginFlag == true) { return RedirectToAction("Onboarding","Account");}

            return View();
        }

        //
        //GET:  Dashboard/Reports
        public ActionResult Reports()
        {
            if (user.FirstLoginFlag == true) { return RedirectToAction("Onboarding", "Account");}

            return View();
        }
    }
}