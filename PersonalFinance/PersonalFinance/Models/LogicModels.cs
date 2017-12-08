using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace PersonalFinance.Models
{
    //Date Picker object class
    public class Dates
    {
        public string start_date { get; set; }
        public string end_date { get; set; }
    }

    //Bar Chart object class
    public class BarChartData
    {
        public string date { get; set; }
        public decimal amount { get; set; }
    }

    //Donut Chart object class
    public class DonutChartData
    {
        public string label { get; set; }
        public decimal value { get; set; }
    }

    //Plaid account metadata object class
    public class AccountData
    {
        public string access_token { get; set; }
        public string institution_name { get; set; }
    }

    //Acount type object class
    public class AccountType
    {
        public string Accounttype { get; set; }
        public string Accounttype_sum { get; set; }
    }

    //Institution object class
    public class Institution
    {
        public string Inst_name { get; set; }
        public string Inst_balance { get; set; }
        public string Inst_access { get; set; }
    }

    //Data Table object class
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
    public class Plaid : IDisposable
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
        private bool disposed = false;
        private List<AccountData> _reauthtoken = new List<AccountData>();
        private List<User_Accounts> _account_list = new List<User_Accounts>();
        private List<User_Transactions> _transaction_list = new List<User_Transactions>();
        private List<BarChartData> _barChart = new List<BarChartData>();

        public ApplicationUser User { get; set; }
        public List<User_Accounts> Account_list
        {
            get { return _account_list; }
            set { _account_list = value; }
        }
        public List<User_Transactions> Transaction_list
        {
            get { return _transaction_list; }
            set { _transaction_list = value; }
        }
        public List<BarChartData> BarChart
        {
            get { return _barChart; }
            set { _barChart = value; }
        }
        public List<DonutChartData> DonutChart = new List<DonutChartData>();
        public string Start_date { get; set; }
        public string End_date { get; set; }
        public bool Has_accounts { get; set; } = false;
        public string Institution_name { get; set; }
        public decimal SumTransactions { get; set; } = 0;
        public string NetWorth { get; set; }
        public string Assets { get; set; }
        public string Liabilities { get; set; }
        public List<AccountType> AccountTypeList = new List<AccountType>();
        public List<Institution> InstitutionList = new List<Institution>();
        public string SelectedAccount { get; set; } = "All Accounts";
        public List<AccountData> Reauthaccounts = new List<AccountData>();
        public string p_token { get; set; } = "";

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

            string data = String.Format("{{ \"client_id\":\"{0}\" , \"secret\":\"{1}\" , \"public_token\":\"{2}\" }}", _clientid, _secret, _public_token);
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
        //Initial account pull from Plaid. Pulls and persists all accounts selected with the authenticated institution
        private async Task AddAccounts()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/accounts/get");

            string data = String.Format("{{ \"client_id\":\"{0}\" , \"secret\":\"{1}\" , \"access_token\":\"{2}\" }}", _clientid, _secret, _accesstoken);
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
                var tokenquery = from db in context.User_Items
                                 where db.ID.Equals(User.Id)
                                 select new { db.Access_Token, db.Institution_Name };

                foreach (var token in tokenquery)
                {
                    AccountData data = new AccountData
                    {
                        access_token = token.Access_Token.ToString(),
                        institution_name = token.Institution_Name.ToString()
                    };

                    _accesstokenlist.Add(data);
                }
            }
        }

        //exchange list of access tokens for public tokens
        private void PublicTokenExchange(List<AccountData> _tokenlist)
        {
            if (client.BaseAddress is null)
            {
                client.BaseAddress = new Uri(_baseurl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            
            foreach (var token in _tokenlist)
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/item/public_token/create");

                string data = String.Format("{{ \"client_id\":\"{0}\" , \"secret\":\"{1}\" , \"access_token\":\"{2}\" }}", _clientid, _secret, token.access_token);
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                var result = client.SendAsync(request).Result;
                var contents = result.Content.ReadAsStringAsync().Result;
                var obj = JObject.Parse(contents);

                Reauthaccounts.Add(new AccountData
                {
                    access_token = (string)obj["public_token"],
                    institution_name = token.institution_name
                });
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
            decimal _Assets = 0;
            decimal _Liabilities = 0;

            foreach (var token in _accesstokenlist)
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/accounts/get");

                string data = String.Format("{{ \"client_id\":\"{0}\" , \"secret\":\"{1}\" , \"access_token\":\"{2}\" }}", _clientid, _secret, token.access_token);
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                var result = client.SendAsync(request).Result;
                var contents = result.Content.ReadAsStringAsync().Result;
                var obj = JObject.Parse(contents);

                try
                {
                    //check for auth error and add to list of auth tokens to be reauthenticated with Plaid
                    if (((string)obj["error_code"]).Equals("ITEM_LOGIN_REQUIRED"))
                    {
                        _reauthtoken.Add(token);
                        continue;
                    }
                }
                catch
                {
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

                            _account_list.Add(accounts_db);

                            Has_accounts = true;

                            if (accounts_db.Account_Type.Equals("mortgage"))
                            {
                                continue;
                            }
                            else if (accounts_db.Account_Type.Equals("credit") || accounts_db.Account_Type.Equals("loan"))
                            {
                                _Liabilities += (decimal)accounts_db.Balance;
                                _NetWorth -= (decimal)accounts_db.Balance;
                            }
                            else
                            {
                                _Assets += (decimal)accounts_db.Balance;
                                _NetWorth += (decimal)accounts_db.Balance;
                            }

                        }

                    }
                }
            }

            if (_reauthtoken.Count != 0)
            {
                PublicTokenExchange(_reauthtoken);
            }

            else
            {
                Assets = String.Format("{0:C}", _Assets);
                Liabilities = String.Format("{0:C}", _Liabilities);
                NetWorth = String.Format("{0:C}", _NetWorth);

                var accountquery = from db in _account_list
                                   group db by db.Account_Type into g
                                   select new
                                   {
                                       Type = g.Distinct(),
                                       Sum = g.Sum(s => s.Balance)
                                   };

                foreach (var type in accountquery)
                {
                    AccountType aAccountType = new AccountType
                    {
                        Accounttype = type.Type.FirstOrDefault().Account_Type.ToString(),
                        Accounttype_sum = String.Format("{0:C}", type.Sum)
                    };

                    AccountTypeList.Add(aAccountType);
                }

                var institutionquery = from db in _account_list
                                       group db by db.Institution_name into g
                                       select new
                                       {
                                           Name = g.Distinct(),
                                           Sum = g.Sum(s => s.Balance)
                                       };
                foreach (var inst in institutionquery)
                {
                    Institution aInstitution = new Institution
                    {
                        Inst_name = inst.Name.FirstOrDefault().Institution_name.ToString(),
                        Inst_balance = String.Format("{0:C}", inst.Sum),
                        Inst_access = inst.Name.FirstOrDefault().Access_Token.ToString()
                    };

                    InstitutionList.Add(aInstitution);
                }
            }
        }

        private void GetAccountIDList(string account_id)
        {
            using (var context = new PersonalFinanceAppEntities())
            {
                if (account_id is null)
                {
                    //go to database and get list of all account ID's assoicated with a user and save to _accountidlist
                    var account_id_query = from db in context.User_Accounts
                                           where db.UserID == User.Id
                                           select db.AccountID;

                    foreach (var accountid in account_id_query)
                    {
                        _accountidlist.Add(accountid);
                    }
                }
                else
                {
                    //we know the specific account ID so just add it to the 'list' of account Id's
                    _accountidlist.Add(account_id);

                    var account_query = from db in context.User_Accounts
                                        where db.AccountID == account_id
                                        select db.AccountName;
                    foreach (var name in account_query)
                    {
                        SelectedAccount = name.ToString();
                    }

                }
            }
        }

        //
        //Method that will return a list of transactions for each account in the account list for a given timeframe
        //and populates the data for the charts for the main dashboard
        //Rather than overload, if dates not known, default range of 1 month set
        public void GetTransactions(DateTime? start_date = null, DateTime? end_date = null, string account_id = null)
        {
            DateTime S_date = start_date ?? DateTime.Today.AddMonths(-1);
            DateTime E_date = end_date ?? DateTime.Today;

            Start_date = S_date.ToShortDateString();
            End_date = E_date.ToShortDateString();

            GetAccountIDList(account_id);

            //parse list of account id's and get list of transactions for each account where the transactions are
            //between the specified dates
            foreach (var accountid in _accountidlist)
            {
                GetTransactionList(S_date, E_date, accountid);
            }

            using (var context = new PersonalFinanceAppEntities())
            {
                if (_transaction_list != null)
                {
                    PopulateCharts(S_date, E_date);
                }

            }
        }

        private void GetTransactionList(DateTime S_date, DateTime E_date, string accountid)
        {
            using (var context = new PersonalFinanceAppEntities())
            {
                var transaction_query = from db in context.User_Transactions
                                        where accountid == db.AccountID
                                        && db.Date >= S_date
                                        && db.Date <= E_date
                                        orderby (db.Date)
                                        select new
                                        {
                                            db.Date,
                                            category = (
                                                        from test in context.Transaction_Categories
                                                        where test.CategoryID == db.CategoryID
                                                        select new { test.Hierarchy }
                                                        ),
                                            db.Location_Name,
                                            db.Location_City,
                                            db.Location_State,
                                            db.Amount
                                        };

                //create list of transaction objects
                foreach (var t in transaction_query)
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

                    _transaction_list.Add(aTransaction);
                }
            }
        }

        private void PopulateCharts(DateTime S_date, DateTime E_date)
        {
            //if the time frame selected is greater than 31 days, condense chart to transaction totals by month
            if ((E_date - S_date).TotalDays > 31)
            {
                var BarChartquery = from transaction in _transaction_list
                                    where transaction.Amount > 0
                                    group transaction by transaction.Date.Month into g
                                    select new
                                    {
                                        Date = g.Distinct(),
                                        Amount = g.Sum(s => s.Amount)
                                    };
                BarChartquery.OrderBy(x => x.Date);

                foreach (var datapoint in BarChartquery)
                {
                    BarChartData aDataPoint = new BarChartData
                    {
                        amount = datapoint.Amount,
                        date = datapoint.Date.Select(t => t.Date.ToString("MMMM")).FirstOrDefault()
                    };

                    _barChart.Add(aDataPoint);
                }
            }

            else
            {
                var BarChartquery = from transaction in _transaction_list
                                    where transaction.Amount > 0
                                    group transaction by new { transaction.Date.Day } into g
                                    select new
                                    {
                                        Date = g.Distinct(),
                                        Amount = g.Sum(s => s.Amount)
                                    };
                BarChartquery.OrderBy(x => x.Date);
                foreach (var datapoint in BarChartquery)
                {
                    BarChartData aDataPoint = new BarChartData
                    {
                        amount = datapoint.Amount,
                        date = datapoint.Date.Select(t => t.Date.ToString("MM-dd")).FirstOrDefault()
                    };

                    _barChart.Add(aDataPoint);
                }
            }

            //code to pull out list of unique dates and sum of transactions per date for donut chart
            var DonutChartquery = from transaction in _transaction_list
                                  where transaction.Amount > 0
                                  group transaction by new { transaction.CategoryID } into g
                                  select new
                                  {
                                      Category = g.Distinct(),
                                      Amount = g.Sum(s => s.Amount)
                                  };

            //get the sum of all non-negative transaction for our pie chart
            foreach (var item in DonutChartquery)
            {
                DonutChartData aDatapoint = new DonutChartData
                {
                    value = item.Amount,
                    label = item.Category.Select(t => t.CategoryID.ToString()).FirstOrDefault()
                };

                SumTransactions += aDatapoint.value;

                DonutChart.Add(aDatapoint);
                DonutChart.Sort((x, y) => x.value.CompareTo(y.value));
            }
        }

        //
        //Method to get the sum of transactions for the dashboard pie chart
        public void DonutDataSum(List<DonutChartData> chartdata)
        {
            SumTransactions = 0;

            foreach (var datapoint in chartdata)
            {
                SumTransactions += datapoint.value;
            }
        }

        //
        //UI button click -> Azure Delete function -> activate DB sproc
        public async Task DeleteInstitution(string access_token)
        {
            _accesstoken = access_token;

            //Get list of accounts associated with access token from DB
            using (var context = new PersonalFinanceAppEntities())
            {
                var token = from db in context.User_Accounts
                            where db.Access_Token.Equals(access_token)
                            select new { db.AccountID };

                List<string> accountid_list = new List<string>();
                foreach (var t in token)
                {
                    accountid_list.Add(t.AccountID.ToString());
                }

                //Pass off the access token to azure function to delete account with Plaid.
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_Delete?code=itYQTwDowYyFt9/6awQ5VIUtmMl2LPscgWb5IeEa48avuTN2ftt6cQ==");

                string item_id_data = String.Format("{{ \"access_token\":\"{0}\" }}", _accesstoken);

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
                        GroupName = (string)category["hierarchy"].First(),
                        Hierarchy = (string)category["hierarchy"].Last()
                    };

                    context.Transaction_Categories.Add(transaction_category);
                    context.SaveChanges();
                }
            }
        }

        //
        //TODO: FUnction that will delete all transactions, accounts, items and user data in DB
        //and disconnect all accounts from Plaid webhooks
        public void DeleteAccount()
        {
            //Get list of all access tokens associated with this user

            //Get list of all accounts associate with each access token

            //Pass off each token to azure function to delete account with Plaid

            //If everything is cool with the function (200 response), delete the account and all transactions related to the account
            //from the DB

            //Once all accounts and transactions have been deleted from Plaid and the DB, delete the user data from DB

            //......maybe we can just wrap all of the DB stuff into one SProc?
        }

        //TODO
        //Method to calculate goal track success, determine if the user is 'on track', calculate year to date
        //savings, ratio of savings to goal amount for front end
        public void GoalProgress()
        {

        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    client.Dispose();
                }

                // Note disposing has been done.
                disposed = true;

            }
        }
    }
}