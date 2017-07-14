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

        //Home dashboard
        public ActionResult Main()
        {
            return View();
        }

        //Reports view and download
        public ActionResult Reports()
        {
            return View();
        }
    }
}