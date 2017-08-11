using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Configuration;

namespace PersonalFinance.Models
{
    public class Dates
    {
        public string start_date { get; set; }
        public string end_date { get; set; }
    }

    public class Plaid
    {
        private static string _clientid = WebConfigurationManager.AppSettings["client_id"];
        private static string _secret = WebConfigurationManager.AppSettings["secret"];
        private static string _baseurl = "https://sandbox.plaid.com";
        private HttpClient client = new HttpClient();
        private string _accesstoken;
        private string _item_id;
        private string _public_token;
        private List<string> _accesstokenlist = new List<string>();
        private List<string> _accountidlist = new List<string>();

        public ApplicationUser User { private get; set; }
        public List<User_Accounts> Account_list = new List<User_Accounts>();
        public List<User_Transactions> Transaction_list = new List<User_Transactions>();
        public string start_date;
        public string end_date;
        public bool Has_accounts { get; set; }

        //
        //Sets the Has_accounts to false on object creation for account list view cycle 
        public Plaid() { Has_accounts = false; }

        //
        //Public accessor to create a new account in DB
        public void AuthenticateAccount(string public_token)
        {
            _public_token = public_token;
            this.AuthenticateAccount();
            this.GetTransactions();
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

                context.User_Items.Add(item_db);
                context.SaveChanges();
            }
            Has_accounts = true;
        }

        //
        //Utilizes in memory access_token to get and persist account data and transactions from Plaid to database
        private void GetTransactions()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/transactions/get");

            string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + _accesstoken + "\" , \"start_date\": \"2017-06-28\" , \"end_date\": \"2017-07-27\" }";
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            var result = client.SendAsync(request).Result;
            var contents = result.Content.ReadAsStringAsync().Result;

            var obj = JObject.Parse(contents);

            //check that the item ID's match before continuing on
            if (((string)obj["item"]["item_id"]).Equals(_item_id))
            {
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
            //if we got this far, something went wrong - TO DO: create error message and bomb out of method gracefully
        }

        //
        //Method that will get all access tokens for the current user from the database
        private void GetAccessToken()
        {
            using (var context = new PersonalFinanceAppEntities())
            {
                var token = from db in context.User_Items where db.ID.Equals(User.Id) select db.Access_Token;
                var tokenlist = token.ToList();

                foreach (var t in tokenlist)
                {
                    _accesstokenlist.Add(t.ToString());
                }
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

                string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + token + "\" }";
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
                        Account_list.Add(accounts_db);
                        Has_accounts = true;
                        //To Do:
                        //Update balance in db to new balance where account id's match
                    }
                }
            }
        }

        //
        //Method to delete all accounts related to the specified institution

        //
        //Method that will return a list of transactions for each account in the account list for a given timeframe
        //Amount sums by category, by date, by location etc
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

            //parse list of account id's and get list of transactions for each account
            using (var context = new PersonalFinanceAppEntities())
            {
                foreach (var accountid in _accountidlist)
                {
                    var transaction_query = from db in context.User_Transactions
                                            where accountid == db.AccountID
                                            && db.Date > start_date
                                            && db.Date < end_date
                                            orderby (db.Date)
                                            select new { db.Date, db.CategoryID, db.Location_Name, db.Location_City, db.Location_State, db.Amount };
                    var transaction_list = transaction_query.ToList();

                    //create list of transaction objects
                    foreach (var t in transaction_list)
                    {
                        User_Transactions aTransaction = new User_Transactions();
                        aTransaction.Date = t.Date;
                        aTransaction.CategoryID = t.CategoryID;
                        aTransaction.Location_Name = t.Location_Name;
                        aTransaction.Location_City = t.Location_City;
                        aTransaction.Location_State = t.Location_State;
                        aTransaction.Amount = (decimal)t.Amount;

                        Transaction_list.Add(aTransaction);
                        Transaction_list.Sort((x, y) => x.Date.CompareTo(y.Date));
                    }
                }
            }
        }

        //
        //Method to get all transactions for a specific account for a given timeframe
        private void GetTransactions(DateTime start_date, DateTime end_date, string account_id)
        {

        }
        //
        //Method to pull all *new* transactions from Plaid via webhook
        //aka the "job" method to update DB
    }
}