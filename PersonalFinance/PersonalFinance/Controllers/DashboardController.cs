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
    public class DashboardController : Controller
    {

        private ApplicationDbContext ApplicationDbContext { get; set; }
        private UserManager<ApplicationUser> UserManager { get; set; }
        private ApplicationUser user;

        //
        //Constructor- creates user object the current user that is logged in
        public DashboardController()
        {
            ApplicationDbContext = new ApplicationDbContext();
            UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(ApplicationDbContext));
            user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
        }

        //
        // GET: Dashboard/Main
        //TO DO: figure out how to get default dates to be MTD
        public async Task<ActionResult> Main()
        {
            if (user.FirstLoginFlag == true && user.PhoneNumberConfirmed == false) { return RedirectToAction("AddPhoneNumber", "Manage"); }
            if (user.FirstLoginFlag == true) { return RedirectToAction("AccountViewSync", "Account"); }

            Plaid plaid = new Plaid
            {
                User = user,

                start_date = Session["startdate"] as string,
                end_date = Session["enddate"] as string
            };

            var  transaction_list = Session["transactions"] as List<User_Transactions>;

            if (transaction_list is null)
            {
                plaid.GetTransactions(DateTime.Today.AddMonths(-1), DateTime.Today);
                plaid.start_date = (DateTime.Today.AddMonths(-1).ToShortDateString()).ToString();
                plaid.end_date = DateTime.Today.ToShortDateString().ToString();

                await plaid.GetAccountList();

                var accountlist = plaid.Account_list;
                var chartdata = plaid.BarChart;
                var donutdata = plaid.DonutChart;
                var networth = plaid.NetWorth;

                Session["BarChart"] = chartdata;
                Session["DonutChart"] = donutdata;
                Session["AccountList"] = accountlist;
                Session["NetWorth"] = networth;
            }
            else
            {
                plaid.Transaction_list = transaction_list;
                plaid.BarChart = Session["BarChart"] as List<BarChartData>;
                plaid.DonutChart = Session["DonutChart"] as List<DonutChartData>;
                plaid.Account_list = Session["AccountList"] as List<User_Accounts>;
                plaid.NetWorth = Session["NetWorth"] as List<decimal>;

                plaid.DonutDataSum(plaid.DonutChart);
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
                var chartdata = plaid.BarChart;
                var donutdata = plaid.DonutChart;

                Session["transactions"] = transactions;
                Session["startdate"] = startdate;
                Session["enddate"] = enddate;
                Session["BarChart"] = chartdata;
                Session["DonutChart"] = donutdata;

                plaid.Account_list = Session["AccountList"] as List<User_Accounts>;
                plaid.NetWorth = Session["NetWorth"] as List<decimal>;

                return Json(new { success = true });
            }
            //if we got this far something went wrong and redisplay the page
            return Json(plaid);
        }

        //
        //GET: Populating data for the Data Table
        public JsonResult DataTableHandler(DataTable param)
        {
            Plaid plaid = new Plaid
            {
                User = user,

                start_date = Session["startdate"] as string,
                end_date = Session["enddate"] as string
            };
            var transaction_list = Session["transactions"] as List<User_Transactions>;

            if (transaction_list is null)
            {
                plaid.GetTransactions(DateTime.Today.AddMonths(-1), DateTime.Today);
                plaid.start_date = (DateTime.Today.AddMonths(-1).ToShortDateString()).ToString();
                plaid.end_date = DateTime.Today.ToShortDateString().ToString();

            }
            else
            {
                plaid.Transaction_list = transaction_list;
                plaid.BarChart = Session["BarChart"] as List<BarChartData>;
                plaid.DonutChart = Session["DonutChart"] as List<DonutChartData>;
                plaid.Account_list = Session["AccountList"] as List<User_Accounts>;
                plaid.NetWorth = Session["NetWorth"] as List<decimal>;
                plaid.DonutDataSum(plaid.DonutChart);
            }

            var displayedTransactions = plaid.Transaction_list
                        .Skip(param.iDisplayStart)
                        .Take(param.iDisplayLength);

            var data = from transaction in displayedTransactions
                       select new[] {   transaction.Date.ToShortDateString(),
                                        transaction.CategoryID,
                                        transaction.Location_Name,
                                        (transaction.Location_City+ " "+ transaction.Location_State),
                                        "$"+ transaction.Amount.ToString() };


            return Json(new
            {
                dom = "p<'row'<'col-sm-6'l><'col-sm-6'f>>t<'row'<'col-sm-6'i><'col-sm-6'p>>",
                sEcho = param.sEcho,
                iTotalRecords = plaid.Transaction_list.Count(),
                iTotalDisplayRecords = plaid.Transaction_list.Count(),
                aaData = data
            },
            JsonRequestBehavior.AllowGet);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (UserManager != null)
                {
                    UserManager.Dispose();
                    UserManager = null;
                }
            }

            base.Dispose(disposing);
        }

    }
}