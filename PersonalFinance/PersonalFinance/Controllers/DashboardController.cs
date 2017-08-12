using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using PersonalFinance.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

        //
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
            if (user.FirstLoginFlag == true && user.PhoneNumberConfirmed == false) { return RedirectToAction("AddPhoneNumber", "Manage"); }
            if (user.FirstLoginFlag == true) { return RedirectToAction("GetStarted", "Account"); }

            Plaid plaid = new Plaid();
            plaid.User = user;
            plaid.Transaction_list = Session["transactions"] as List<User_Transactions>;
            plaid.start_date = Session["startdate"] as string;
            plaid.end_date = Session["enddate"] as string;

            if (plaid.Transaction_list is null)
            {
                plaid.GetTransactions(DateTime.Today, DateTime.Today);
                plaid.start_date = (DateTime.Today.ToShortDateString()).ToString();
                plaid.end_date = (DateTime.Today.ToShortDateString()).ToString();
            }

            return View(plaid);
        }

        //
        //POST: Dashboard/Main
        [HttpPost]
        public JsonResult Main(Dates dates)
        {
            Plaid plaid = new Plaid();
            if (ModelState.IsValid)
            {
                DateTime start_date = DateTime.Parse(dates.start_date);
                DateTime end_date = DateTime.Parse(dates.end_date);
                plaid.User = user;
                plaid.GetTransactions(start_date, end_date);

                var transactions = plaid.Transaction_list;
                var startdate = dates.start_date;
                var enddate = dates.end_date;
                Session["transactions"] = transactions;
                Session["startdate"] = startdate;
                Session["enddate"] = enddate;

                return Json(new { success = true });
            }
            //if we got this far something went wrong and redisplay the page
            return Json(plaid);
        }

        //
        //GET:  Dashboard/Reports
        public ActionResult Reports()
        {
            if (user.FirstLoginFlag == true && user.PhoneNumberConfirmed == false) { return RedirectToAction("AddPhoneNumber", "Manage"); }
            if (user.FirstLoginFlag == true) { return RedirectToAction("GetStarted", "Account"); }

            return View();
        }
    }
}