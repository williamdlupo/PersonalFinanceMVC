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

        public ApplicationUser User { private get; set; }
        public List<User_Accounts> Account_list = new List<User_Accounts>();
        public bool Has_accounts { get; set; }

        public Plaid() { Has_accounts = false; }

        public void AuthenticateAccount(string public_token)
        {
            _public_token = public_token;
            this.AuthenticateAccount();
            this.GetTransactions();
        }

        //
        //Authenticates a new User account with Plaid, stores in memory and persists the access token and item id
        //associated with the account to the database
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
                item_db.Access_Token = _accesstokenlist[0];
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
        //Method that will return a list of transactions for each account in the account list for a given timeframe
        //Amount sums by category, by date, by location etc

        //
        //Method to get all transactions for a specific account for a given timeframe

        //
        //Method to pull all *new* transactions from Plaid for all items associated with all  access codes 
        //aka the "job" method to update DB
    }
}