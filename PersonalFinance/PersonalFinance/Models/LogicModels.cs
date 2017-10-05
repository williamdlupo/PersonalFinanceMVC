using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace PersonalFinance.Models
{
    //Date Picker data type class
    public class Dates
    {
        public string start_date { get; set; }
        public string end_date { get; set; }
    }

    //Morris Chart data type class
    public class BarChartData
    {
        public string date { get; set; }
        public decimal amount { get; set; }
    }

    //Morris Donut Chart data type class
    public class DonutChartData
    {
        public string label { get; set; }
        public decimal value { get; set; }
    }

    //Plaid metadata data type class
    public class AccountData
    {
        public string access_token { get; set; }
        public string institution_name { get; set; }
    }

    //Data Table data type class
    public class DataTable
    {
        //Request sequence number sent by DataTable,
        //Same value must be returned in response      
        public string sEcho { get; set; }

        //Text used for filtering        
        public string sSearch { get; set; }

        /// Number of records that should be shown in table
        public int iDisplayLength { get; set; }

        //First record that should be shown(used for paging)        
        public int iDisplayStart { get; set; }

        //Number of columns in table
        public int iColumns { get; set; }

        //Number of columns that are used in sorting        
        public int iSortingCols { get; set; }

        /// Comma separated list of column names
        public string sColumns { get; set; }
    }

    //Plaid class where the 'magic' happens
    public class Plaid
    {
        private static string _clientid = WebConfigurationManager.AppSettings["client_id"];
        private static string _secret = WebConfigurationManager.AppSettings["secret"];
        private static string _baseurl = "https://development.plaid.com";
        private HttpClient client = new HttpClient();
        private string _accesstoken;
        private string _item_id;
        private string _public_token;
        private List<string> _accountidlist = new List<string>();
        private List<AccountData> _accesstokenlist = new List<AccountData>();

        public ApplicationUser User { get; set; }
        public List<User_Accounts> Account_list = new List<User_Accounts>();
        public List<User_Transactions> Transaction_list = new List<User_Transactions>();
        public List<BarChartData> BarChart = new List<BarChartData>();
        public List<DonutChartData> DonutChart = new List<DonutChartData>();
        public string start_date;
        public string end_date;
        public bool Has_accounts { get; set; }
        public string Institution_name { get; set; }
        public decimal SumTransactions { get; set; }
        public List<decimal> NetWorth = new List<decimal>();
        public List<string> AccountTypeList = new List<string>();

        //
        //Sets the Has_accounts to false on object creation for account list view cycle 
        public Plaid() { Has_accounts = false; }

        //
        //Public accessor to create a new account in DB
        public async Task AuthenticateAccount(string public_token)
        {
            _public_token = public_token;
            await AuthenticateAccount();
            await AddAccounts();
        }

        //
        //Authenticates a new User account with Plaid, stores access token and item IDs in memory and persists to DB
        private async Task AuthenticateAccount()
        {
            client.BaseAddress = new Uri(_baseurl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/item/public_token/exchange");

            string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"public_token\":\"" + _public_token + "\" }";
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            var result = client.SendAsync(request).Result;
            var contents = result.Content.ReadAsStringAsync().Result;

            var obj = JObject.Parse(contents);
            var url = (string)obj["access_token"];
            _accesstoken = url.ToString();
            url = (string)obj["item_id"];
            _item_id = url.ToString();

            using (var context = new PersonalFinanceAppEntities())
            {
                User_Items item_db = new User_Items
                {
                    Access_Token = _accesstoken,
                    Item_ID = _item_id,
                    ID = User.Id,
                    Institution_Name = Institution_name.ToString()
                };

                context.User_Items.Add(item_db);
                await context.SaveChangesAsync();
            }
            Has_accounts = true;
        }

        //
        //Method to pull all current transaction categroy information from Plaid and save to database
        public async Task GetCategories()
        {

            if (client.BaseAddress is null)
            {
                client.BaseAddress = new Uri(_baseurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/categories/get");

            string data = "{ }";
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            var connectasync = await client.SendAsync(request);
            var contents = connectasync.Content.ReadAsStringAsync().Result;

            var obj = JObject.Parse(contents);

            using (var context = new PersonalFinanceAppEntities())
            {
                foreach (var category in obj["categories"])
                {
                    Transaction_Categories transaction_category = new Transaction_Categories
                    {
                        CategoryID = (string)category["category_id"],
                        GroupName = (string)category["group"],
                        Hierarchy = (string)category["hierarchy"].Last()
                    };

                    context.Transaction_Categories.Add(transaction_category);
                    context.SaveChanges();
                }
            }
        }

        //
        //Initial account and transaction pull from Plaid. Gets 3 months worth of transactions per each account 
        private async Task AddAccounts()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/accounts/get");

            string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + _accesstoken + "\" }";
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            var connectasync = await client.SendAsync(request);
            var contents = connectasync.Content.ReadAsStringAsync().Result;

            var obj = JObject.Parse(contents);

            //parse the JSON object extract account data and persist to database
            using (var context = new PersonalFinanceAppEntities())
            {
                foreach (var account in obj["accounts"])
                {
                    User_Accounts accounts_db = new User_Accounts
                    {
                        AccountID = (string)account["account_id"],
                        UserID = User.Id,
                        AccountName = (string)account["official_name"],
                        Balance = (decimal)account["balances"]["current"],
                        Institution_name = this.Institution_name,
                        Access_Token = _accesstoken,
                        Account_Type = (string)account["type"]
                    };

                    context.User_Accounts.Add(accounts_db);
                    await context.SaveChangesAsync();
                }
            }
        }

        //
        //Method that will get all access tokens for the current user from the database
        private void GetAccessToken()
        {
            using (var context = new PersonalFinanceAppEntities())
            {
                var token = from db in context.User_Items
                            where db.ID.Equals(User.Id)
                            select new { db.Access_Token, db.Institution_Name };

                var tokenlist = token.ToList();

                foreach (var t in tokenlist)
                {
                    AccountData data = new AccountData
                    {
                        access_token = t.Access_Token.ToString(),
                        institution_name = t.Institution_Name.ToString()
                    };

                    _accesstokenlist.Add(data);
                }
            }
        }

        //
        //Method that will kick back list that contains the account name and balance 
        //for each account for current User
        public async Task GetAccountList()
        {
            if (_accesstoken is null) { this.GetAccessToken(); }

            if (client.BaseAddress is null)
            {
                client.BaseAddress = new Uri(_baseurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            decimal _NetWorth = 0;

            foreach (var token in _accesstokenlist)
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/accounts/get");

                string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + token.access_token + "\" }";
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                var result = client.SendAsync(request).Result;
                var contents = result.Content.ReadAsStringAsync().Result;

                var obj = JObject.Parse(contents);

                //parse the JSON object extract account data to memory and persist to database
                using (var context = new PersonalFinanceAppEntities())
                {
                    foreach (var account in obj["accounts"])
                    {
                        User_Accounts accounts_db = new User_Accounts
                        {
                            AccountID = (string)account["account_id"],
                            AccountName = (string)account["official_name"],
                            Balance = (decimal)account["balances"]["current"],
                            Institution_name = token.institution_name,
                            Account_Type = (string)account["type"],
                            Access_Token = token.access_token
                        };

                        //Stored procedure to update account balance in DB with matching account ID.
                        context.Update_AccountBalance(accounts_db.AccountID, accounts_db.Balance);
                        await context.SaveChangesAsync();

                        Account_list.Add(accounts_db);

                        Has_accounts = true;

                        if (accounts_db.Account_Type.Equals("credit") || accounts_db.Account_Type.Equals("loan") || accounts_db.Account_Type.Equals("mortgage"))
                        {
                            _NetWorth -= (decimal)accounts_db.Balance;
                        }
                        else
                        {
                            _NetWorth += (decimal)accounts_db.Balance;
                        }

                    }

                }
            }
            NetWorth.Add(_NetWorth);

            var accountquery = from db in Account_list
                               orderby db.Account_Type, db.Institution_name, db.AccountName
                               select new { db.Account_Type, db.Institution_name, db.AccountName };

            var accountlist = accountquery.ToList();

            //get list of distinct account types
            var _accounttype = from mem in accountlist
                               orderby mem.Account_Type
                               select new { mem.Account_Type };
            var _accounttypelst = _accounttype.ToList().Distinct();

            foreach (var type in _accounttypelst)
            {
                AccountTypeList.Add(type.Account_Type.ToString());
            }
        }

        //
        //Method that will return a list of transactions for each account in the account list for a given timeframe
        //and populates the data for the charts for the main dashboard
        public void GetTransactions(DateTime start_date, DateTime end_date)
        {
            //go to database and get list of account ID's assoicated with a user and save to _accountidlist
            using (var context = new PersonalFinanceAppEntities())
            {
                var account_id_query = from db in context.User_Accounts
                                       where db.UserID == User.Id
                                       select db.AccountID;
                var account_id_list = account_id_query.ToList();

                foreach (var accountid in account_id_query)
                {
                    _accountidlist.Add(accountid);
                }
            }

            //parse list of account id's and get list of transactions for each account where the transactions are
            //between the specified dates
            using (var context = new PersonalFinanceAppEntities())
            {
                foreach (var accountid in _accountidlist)
                {
                    var transaction_query = from db in context.User_Transactions
                                            where accountid == db.AccountID
                                            && db.Date >= start_date
                                            && db.Date <= end_date
                                            orderby (db.Date)
                                            select new { db.Date, category = (from test in context.Transaction_Categories where test.CategoryID == db.CategoryID select new { test.Hierarchy }), db.Location_Name, db.Location_City, db.Location_State, db.Amount };
                    var transaction_list = transaction_query.ToList();

                    //create list of transaction objects and sort by date
                    foreach (var t in transaction_list)
                    {
                        User_Transactions aTransaction = new User_Transactions
                        {
                            Date = t.Date,
                            Location_Name = t.Location_Name,
                            Location_City = t.Location_City,
                            Location_State = t.Location_State,
                            Amount = t.Amount
                        };

                        foreach (var item in t.category)
                        {
                            aTransaction.CategoryID = item.Hierarchy;
                        }

                        if (aTransaction.CategoryID is null)
                        {
                            aTransaction.CategoryID = "Unknown";
                        }
                        Transaction_list.Add(aTransaction);
                        Transaction_list.Sort((x, y) => x.Date.CompareTo(y.Date));
                    }
                }

                if (Transaction_list != null)
                {
                    //if the time frame selected is greater than 31 days, condense chart to transaction totals by month
                    if ((end_date - start_date).TotalDays > 31)
                    {
                        var BarChartquery = from transaction in Transaction_list
                                            where transaction.Amount > 0
                                            group transaction by new { transaction.Date.Month } into g
                                            select new
                                            {
                                                Date = g.Distinct(),
                                                Amount = g.Sum(s => s.Amount)
                                            };
                        var BarChartData = BarChartquery.ToList();

                        foreach (var datapoint in BarChartData)
                        {
                            BarChartData aDataPoint = new BarChartData
                            {
                                amount = datapoint.Amount
                            };

                            foreach (var date in datapoint.Date)
                            {
                                aDataPoint.date = date.Date.ToString("MMMM");
                                break;
                            }
                            BarChart.Add(aDataPoint);
                        }
                    }

                    else
                    {
                        //code to pull out list of unique dates and sum of transactions per date for bar chart
                        var BarChartquery = from transaction in Transaction_list
                                            where transaction.Amount > 0
                                            group transaction by new { transaction.Date } into g
                                            select new
                                            {
                                                Date = g.Distinct(),
                                                Amount = g.Sum(s => s.Amount)
                                            };
                        var BarChartData = BarChartquery.ToList();

                        foreach (var datapoint in BarChartData)
                        {
                            BarChartData aDataPoint = new BarChartData
                            {
                                amount = datapoint.Amount
                            };

                            foreach (var date in datapoint.Date)
                            {
                                aDataPoint.date = date.Date.ToString("MM-dd");
                                break;
                            }
                            BarChart.Add(aDataPoint);
                        }
                    }

                    //code to pull out list of unique dates and sum of transactions per date for donut chart
                    var DonutChartquery = from transaction in Transaction_list
                                          group transaction by new { transaction.CategoryID } into g
                                          select new
                                          {
                                              Category = g.Distinct(),
                                              Amount = g.Sum(s => s.Amount)
                                          };
                    var DonutChartList = DonutChartquery.ToList();
                    //get the sum of all non-negative transaction for our pie chart
                    SumTransactions = 0;
                    foreach (var item in DonutChartList)
                    {
                        DonutChartData aDatapoint = new DonutChartData();

                        if (item.Amount < 0) { continue; }
                        aDatapoint.value = item.Amount;
                        SumTransactions += aDatapoint.value;

                        foreach (var aCat in item.Category)
                        {
                            aDatapoint.label = aCat.CategoryID.ToString();
                            break;
                        }
                        DonutChart.Add(aDatapoint);
                        DonutChart.Sort((x, y) => x.value.CompareTo(y.value));
                    }
                }

            }
        }

        //
        //Method to get the sum of transactions for the pie chart data
        public void DonutDataSum(List<DonutChartData> chartdata)
        {
            SumTransactions = 0;

            foreach (var datapoint in chartdata)
            {
                SumTransactions += datapoint.value;
            }
        }

        //
        //Stored Procedure has been created, just have to tie sproc into code and front end UI
        public async Task DeleteInstitution(string access_token)
        {
            _accesstoken = access_token;

            //Get list of accounts associated with access token from DB
            using (var context = new PersonalFinanceAppEntities())
            {
                var token = from db in context.User_Accounts
                            where db.Access_Token.Equals(access_token)
                            select new { db.AccountID };

                var tokenlist = token.ToList();

                List<string> accountid_list = new List<string>();
                foreach (var t in tokenlist)
                {
                    accountid_list.Add(t.AccountID.ToString());
                }

                //Pass off the access token to azure function to delete account with Plaid.
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_Delete?code=itYQTwDowYyFt9/6awQ5VIUtmMl2LPscgWb5IeEa48avuTN2ftt6cQ==");

                string item_id_data = "{ \"access_token\":\"" + _accesstoken + "\" }";

                request.Content = new StringContent(item_id_data);

                var access_token_connectasync = await client.SendAsync(request);

                //If everything is cool with the function (200 response), delete the account and all transactions related to that account
                //from DB
                if (access_token_connectasync.IsSuccessStatusCode)
                {
                    foreach (var account in accountid_list)
                    {
                        context.DeleteTransactions(account);
                    }

                    context.DeleteAccount(access_token);

                    await context.SaveChangesAsync();
                }
            }
        }

        //TODO
        //Method to get all transactions for a specific account for a given timeframe
        //This is a pretty big undertaking....
        private void GetTransactions(DateTime start_date, DateTime end_date, string account_id)
        {

        }


        //TODO
        //Method to calculate goal track success, determine if the user is 'on track', calculate year to date
        //savings, ratio of savings to goal amount for front end
        public void GoalProgress()
        {

        }
    }
}