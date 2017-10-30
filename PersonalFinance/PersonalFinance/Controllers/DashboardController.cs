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
            if (user.FirstLoginFlag == true) { return RedirectToAction("AccountSync", "Account"); }

            Plaid plaid = new Plaid
            {
                User = user,

                Start_date = Session["startdate"] as string,
                End_date = Session["enddate"] as string
            };

            var transaction_list = Session["transactions"] as List<User_Transactions>;

            if (transaction_list is null)
            {
                plaid.GetTransactions();

                await plaid.GetAccountList();

                Session["BarChart"] = plaid.BarChart;
                Session["DonutChart"] = plaid.DonutChart;
                Session["AccountList"] = plaid.Account_list;
                Session["NetWorth"] = plaid.NetWorth;
                Session["InstitutionList"] = plaid.InstitutionList;
            }
            else
            {
                plaid.Transaction_list = transaction_list;
                plaid.BarChart = Session["BarChart"] as List<BarChartData>;
                plaid.DonutChart = Session["DonutChart"] as List<DonutChartData>;
                plaid.Account_list = Session["AccountList"] as List<User_Accounts>;
                plaid.NetWorth = Session["NetWorth"] as List<decimal>;
                plaid.SelectedAccount = Session["SelectedAccount"] as string;
                plaid.InstitutionList = Session["InstitutionList"] as List<Institution>;

                plaid.DonutDataSum(plaid.DonutChart);
            }

            return View(plaid);
        }

        //
        //POST: Dashboard/Main
        [HttpPost]
        public JsonResult DatePickerHandler(Dates dates)
        {
            Plaid plaid = new Plaid();

            if (ModelState.IsValid)
            {
                DateTime start_date = DateTime.Parse(dates.start_date);
                DateTime end_date = DateTime.Parse(dates.end_date);
                plaid.User = user;

                plaid.GetTransactions(start_date, end_date);

                Session["transactions"] = plaid.Transaction_list;
                Session["startdate"] = dates.start_date;
                Session["enddate"] = dates.end_date;
                Session["BarChart"] = plaid.BarChart;
                Session["DonutChart"] = plaid.DonutChart;
                Session["SelectedAccount"] = plaid.SelectedAccount;

                plaid.Account_list = Session["AccountList"] as List<User_Accounts>;
                plaid.NetWorth = Session["NetWorth"] as List<decimal>;
                plaid.InstitutionList = Session["InstitutionList"] as List<Institution>;

                return Json(new { success = true });
            }
            //if we got this far something went wrong - redisplay the page
            return Json(plaid);
        }

        //
        //GET: Populating data for the Data Table
        public JsonResult DataTableHandler(DataTable param)
        {
            Plaid plaid = new Plaid
            {
                User = user,

                Start_date = Session["startdate"] as string,
                End_date = Session["enddate"] as string
            };
            var transaction_list = Session["transactions"] as List<User_Transactions>;

            if (transaction_list is null)
            {
                plaid.GetTransactions();

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

            var displayedTransactions = (IEnumerable<User_Transactions>)plaid.Transaction_list;

            var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);

            if (sortColumnIndex == 0)
            {
                Func<User_Transactions, DateTime> orderingFunction = (c => c.Date);
                var sortDirection = Request["sSortDir_0"]; // asc or desc
                if (sortDirection == "desc")
                    displayedTransactions = displayedTransactions.OrderBy(orderingFunction);
                else
                    displayedTransactions = displayedTransactions.OrderByDescending(orderingFunction);
            }
            else if(sortColumnIndex == 4)
            {
                Func<User_Transactions, decimal> orderingFunction = (c => c.Amount);
                var sortDirection = Request["sSortDir_0"]; // asc or desc
                if (sortDirection == "asc")
                    displayedTransactions = displayedTransactions.OrderBy(orderingFunction);
                else
                    displayedTransactions = displayedTransactions.OrderByDescending(orderingFunction);
            }
            else
            {
                Func<User_Transactions, string> orderingFunction = (c => sortColumnIndex == 1 ? c.CategoryID :
                                                               sortColumnIndex == 2 ? c.Location_Name :
                                                               c.Location_State);
                var sortDirection = Request["sSortDir_0"]; // asc or desc
                if (sortDirection == "asc")
                    displayedTransactions = displayedTransactions.OrderBy(orderingFunction);
                else
                    displayedTransactions = displayedTransactions.OrderByDescending(orderingFunction);
            }

            displayedTransactions = displayedTransactions
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
                dom = "",
                sEcho = param.sEcho,
                iTotalRecords = plaid.Transaction_list.Count(),
                iTotalDisplayRecords = plaid.Transaction_list.Count(),
                iSortingCols = 5,
                aaData = data
            },
            JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> AccountViewHandler(string trim)
        {
            Plaid plaid = new Plaid
            {
                User = user,

                Start_date = Session["startdate"] as string,
                End_date = Session["enddate"] as string
            };

            try
            {
                DateTime start_date = DateTime.Parse(plaid.Start_date);
                DateTime end_date = DateTime.Parse(plaid.End_date);

                plaid.GetTransactions(start_date, end_date, trim);
            }

            catch
            {
                DateTime? start_date = null;
                DateTime? end_date = null;

                plaid.GetTransactions(start_date, end_date, trim);
            }

            await plaid.GetAccountList();

            var accountlist = plaid.Account_list;
            var chartdata = plaid.BarChart;
            var donutdata = plaid.DonutChart;
            var networth = plaid.NetWorth;

            Session["BarChart"] = chartdata;
            Session["DonutChart"] = donutdata;
            Session["AccountList"] = accountlist;
            Session["NetWorth"] = networth;
            Session["transactions"] = plaid.Transaction_list;

            return View("Main", plaid);
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