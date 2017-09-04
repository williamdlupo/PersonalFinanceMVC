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
        public int value { get; set; }
    }

    //Plaid metadata data type class
    public class AccountData
    {
        public string access_token { get; set; }
        public string institution_name { get; set; }
    }

    //Date Table data type class
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
        private List<AccountData> _accesstokenlist = new List<AccountData>();
        private List<string> _accountidlist = new List<string>();

        public ApplicationUser User { get; set; }
        public List<User_Accounts> Account_list = new List<User_Accounts>();
        public List<User_Transactions> Transaction_list = new List<User_Transactions>();
        public List<BarChartData> BarChart = new List<BarChartData>();
        public List<DonutChartData> DonutChart = new List<DonutChartData>();
        public List<string> Institution_list = new List<string>();
        public string start_date;
        public string end_date;
        public bool Has_accounts { get; set; }
        public string Institution_name { get; set; }

        //
        //Sets the Has_accounts to false on object creation for account list view cycle 
        public Plaid() { Has_accounts = false; }

        //
        //Public accessor to create a new account in DB
        public async Task AuthenticateAccount(string public_token)
        {
            _public_token = public_token;
            this.AuthenticateAccount();
            await this.GetTransactions();
        }

        //
        //Authenticates a new User account with Plaid, stores access token and item IDs in memory and persists to DB
        private void AuthenticateAccount()
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
                User_Items item_db = new User_Items();
                item_db.Access_Token = _accesstoken;
                item_db.Item_ID = _item_id;
                item_db.ID = User.Id;
                item_db.Institution_Name = Institution_name.ToString();

                context.User_Items.Add(item_db);
                context.SaveChanges();
            }
            Has_accounts = true;
        }

        //
        //Initial account and 1 month transaction pull from Plaid
        //TODO: Update the connection string to progamically set 'today' and 'MTD' dates
        private async Task GetTransactions()
        {
            if (_accesstoken is null) { this.GetAccessToken(); }

            if (client.BaseAddress is null)
            {
                client.BaseAddress = new Uri(_baseurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            foreach (var token in _accesstokenlist)
            {

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/transactions/get");

                string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + token.access_token + "\" , \"start_date\": \""+DateTime.Today.AddMonths(-1).ToString("YYYY-MM-DD")+"\" , \"end_date\": \"" + DateTime.Today.ToString("YYYY-MM-DD") + "\" }";
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                var connectasync = await client.SendAsync(request);
                var contents = connectasync.Content.ReadAsStringAsync().Result;

                var obj = JObject.Parse(contents);

                //parse the JSON object extract account data and persist to database
                using (var context = new PersonalFinanceAppEntities())
                {
                    foreach (var account in obj["accounts"])
                    {
                        User_Accounts accounts_db = new User_Accounts();
                        accounts_db.AccountID = (string)account["account_id"];
                        accounts_db.UserID = User.Id;
                        accounts_db.AccountName = (string)account["official_name"];
                        accounts_db.Balance = (decimal)account["balances"]["current"];
                        accounts_db.Institution_name = this.Institution_name;

                        context.User_Accounts.Add(accounts_db);
                        context.SaveChanges();
                    }
                }
                //parse the JSON object extract transactional data and persist to database
                using (var context = new PersonalFinanceAppEntities())
                {
                    foreach (var transaction in obj["transactions"])
                    {
                        User_Transactions transaction_db = new User_Transactions();
                        transaction_db.AccountID = (string)transaction["account_id"];
                        transaction_db.Amount = (decimal)transaction["amount"];
                        transaction_db.CategoryID = (string)transaction["category_id"];
                        transaction_db.Date = (DateTime)transaction["date"];
                        transaction_db.Location_City = (string)transaction["location"]["city"];
                        transaction_db.Location_Name = (string)transaction["name"];
                        transaction_db.Location_State = (string)transaction["location"]["state"];
                        transaction_db.TransactionID = (string)transaction["transaction_id"];

                        context.User_Transactions.Add(transaction_db);
                        context.SaveChanges();
                    }
                }
            }
        }

        //
        //TODO: Pull the YTD transaction list from Plaid in batches of 500 transactions
        public async Task GetYTDTransactions()
        {
            if (_accesstoken is null) { this.GetAccessToken(); }

            if (client.BaseAddress is null)
            {
                client.BaseAddress = new Uri(_baseurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            foreach (var token in _accesstokenlist)
            {

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/transactions/get");

                string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + token.access_token + "\" , \"start_date\": \"2017-01-01\" , \"end_date\": \"2017-08-02\" }";
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                var connectasync = await client.SendAsync(request);
                var contents = connectasync.Content.ReadAsStringAsync().Result;

                var obj = JObject.Parse(contents);

                //parse the JSON object extract transactional data and persist to database
                using (var context = new PersonalFinanceAppEntities())
                {
                    foreach (var transaction in obj["transactions"])
                    {
                        User_Transactions transaction_db = new User_Transactions();
                        transaction_db.AccountID = (string)transaction["account_id"];
                        transaction_db.Amount = (decimal)transaction["amount"];
                        transaction_db.CategoryID = (string)transaction["category_id"];
                        transaction_db.Date = (DateTime)transaction["date"];
                        transaction_db.Location_City = (string)transaction["location"]["city"];
                        transaction_db.Location_Name = (string)transaction["name"];
                        transaction_db.Location_State = (string)transaction["location"]["state"];
                        transaction_db.TransactionID = (string)transaction["transaction_id"];

                        context.User_Transactions.Add(transaction_db);
                        context.SaveChanges();
                    }
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
                    AccountData data = new AccountData();
                    data.access_token = t.Access_Token.ToString();
                    data.institution_name = t.Institution_Name.ToString();
                    Institution_list.Add(t.Institution_Name.ToString());

                    _accesstokenlist.Add(data);
                }
                Institution_list.Sort();
            }
        }

        //
        //Method that will kick back list that contains the account name and balance 
        //for each account for current User
        public void GetAccountList()
        {
            if (_accesstoken is null) { this.GetAccessToken(); }

            if (client.BaseAddress is null)
            {
                client.BaseAddress = new Uri(_baseurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

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
                        User_Accounts accounts_db = new User_Accounts();
                        accounts_db.AccountName = (string)account["official_name"];
                        accounts_db.Balance = (decimal)account["balances"]["current"];
                        accounts_db.Institution_name = token.institution_name;
                        Account_list.Add(accounts_db);
                        this.Has_accounts = true;

                        //To Do:
                        //Update balance in db to new balance where account id's match (sproc update)
                    }
                }
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
                                            select new { db.Date, db.CategoryID, db.Location_Name, db.Location_City, db.Location_State, db.Amount };
                    var transaction_list = transaction_query.ToList();

                    //create list of transaction objects and sort by date
                    foreach (var t in transaction_list)
                    {
                        User_Transactions aTransaction = new User_Transactions();
                        aTransaction.Date = t.Date;
                        aTransaction.CategoryID = t.CategoryID;
                        aTransaction.Location_Name = t.Location_Name;
                        aTransaction.Location_City = t.Location_City;
                        aTransaction.Location_State = t.Location_State;
                        aTransaction.Amount = t.Amount;

                        Transaction_list.Add(aTransaction);
                        Transaction_list.Sort((x, y) => x.Date.CompareTo(y.Date));
                    }
                }

                if (Transaction_list != null)
                {
                    //code to pull out list of unique dates and sum of transactions per date for bar chart
                    var BarChartquery = from transaction in Transaction_list
                                        group transaction by new { transaction.Date } into g
                                        select new
                                        {
                                            Date = g.Distinct(),
                                            Amount = g.Sum(s => s.Amount)
                                        };
                    var BarChartData = BarChartquery.ToList();

                    foreach (var datapoint in BarChartData)
                    {
                        BarChartData aDataPoint = new BarChartData();
                        aDataPoint.amount = datapoint.Amount;

                        foreach (var date in datapoint.Date)
                        {
                            aDataPoint.date = date.Date.ToShortDateString();
                            break;
                        }
                        BarChart.Add(aDataPoint);
                    }

                    //code to pull out list of unique dates and sum of transactions per date for donut chart
                    var DonutChartquery = from transaction in Transaction_list
                                          group transaction by new { transaction.CategoryID } into g
                                          select new
                                          {
                                              Category = g.Distinct(),
                                              Count = g.Count()
                                          };
                    var DonutChartList = DonutChartquery.ToList();

                    foreach (var item in DonutChartList)
                    {
                        DonutChartData aDatapoint = new DonutChartData();
                        aDatapoint.value = item.Count;

                        foreach (var aCat in item.Category)
                        {
                            if (aCat.CategoryID is null)
                            {
                                aDatapoint.label = "Unknown";
                                break;
                            }

                            aDatapoint.label = aCat.CategoryID.ToString();
                            break;
                        }
                        DonutChart.Add(aDatapoint);
                    }
                }

            }
        }


        //TODO
        //Method to delete an account related to the specified institution (stored procedure from DB)
        public void DeleteAccount(string account_id)
        { }

        //TODO
        //Method to get all transactions for a specific account for a given timeframe
        private void GetTransactions(DateTime start_date, DateTime end_date, string account_id)
        {

        }

        //TODO
        //Method to get the total current net worth (sum of all current account balances), 
        //populate the sparkline of the summed net worth per month YTD for a given user 
        public void GetNetWorth()
        {

        }

        //TODO
        //Method to calculate goal track success, determine if the user is 'on track', calculate year to date
        //savings, ratio of savings to goal amount for front end
        public void GoalProgress()
        {

        }

        //TODO
        //Method to pull all *new* transactions from Plaid via webhook
        //aka the "job" method to update DB via the plaid webhook
    }
}