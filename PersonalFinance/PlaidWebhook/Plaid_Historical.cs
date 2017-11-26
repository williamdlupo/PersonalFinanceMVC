using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace PlaidWebhook
{
    public static class Plaid_Historical
    {
        [FunctionName("Plaid_Historical")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            {
                //Update the security protocol so we can use the Plaid API
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                HttpClient first_client = new HttpClient();
                HttpClient client = new HttpClient();
                string access_token = "";
                string _clientid = System.Environment.GetEnvironmentVariable("client_id");
                string _secret = System.Environment.GetEnvironmentVariable("secret");

                // Get the webhook code object from the JSON in the message body.
                var contents = req.Content.ReadAsStringAsync().Result;

                var itemdata = JObject.Parse(contents);

                //parse Webhook JSON for the webhook code and pass item ID to next function
                string item_id = (string)itemdata["item_id"];
                int new_transactions = (int)itemdata["new_transactions"];

                //Pass off itemID to access token swap function
                HttpRequestMessage first_request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_AccessTokenSwap?code=YhKOVBYj/Y4uKoW3aHu7mRwZ3elmuDnKeubawuhUTAwtpWD27ITuEg==");

                string item_id_data = String.Format("{{ \"item_id\":\"{0}\" }}", item_id);

                first_request.Content = new StringContent(item_id_data);

                var access_token_connectasync = await first_client.SendAsync(first_request);
                var access_token_contents = access_token_connectasync.Content.ReadAsStringAsync().Result;

                var access_token_obj = JObject.Parse(access_token_contents);

                access_token = (string)access_token_obj["access_token"];

                //Second we dial into the Plaid API to get the list of transactions for the access token
                client.BaseAddress = new Uri("https://development.plaid.com");

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                int offset = 0;
                int saved = 0;

                while (true)
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/transactions/get");

                    string offsetstring = String.Format("\"offset\": {0}", offset);
                    string data = "{ \"client_id\":\"" + _clientid + "\" , \"secret\":\"" + _secret + "\" , \"access_token\":\"" + access_token + "\" , \"start_date\": \"" + DateTime.Today.AddMonths(-24).ToString("yyyy-MM-dd") + "\" , \"end_date\": \"" + DateTime.Today.AddDays(-31).ToString("yyyy-MM-dd") + "\", \"options\": { \"count\": 500, " + offsetstring + " } }";


                    request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                    var connectasync = await client.SendAsync(request);
                    var result = connectasync.Content.ReadAsStringAsync().Result;

                    var obj = JObject.Parse(result);

                    if (obj["transactions"].Count() < 1) { break; }

                    //Third we handle the result and save the data to the DB
                    var str = ConfigurationManager.ConnectionStrings["sqlConnection"].ConnectionString;
                    
                    foreach (var transaction in obj["transactions"])
                    {
                        try
                        {
                            using (SqlConnection conn = new SqlConnection(str))
                            {
                                using (SqlCommand cmd = new SqlCommand("dbo.Insert_UserTransaction", conn))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.Add("@TransactionID", SqlDbType.VarChar).Value = (string)transaction["transaction_id"];
                                    cmd.Parameters.Add("@AccountID", SqlDbType.VarChar).Value = (string)transaction["account_id"];
                                    cmd.Parameters.Add("@CategoryID", SqlDbType.VarChar).Value = (string)transaction["category_id"];
                                    cmd.Parameters.Add("@Date", SqlDbType.DateTime).Value = (DateTime)transaction["date"];
                                    cmd.Parameters.Add("@Location_City", SqlDbType.VarChar).Value = (string)transaction["location"]["city"];
                                    cmd.Parameters.Add("@Location_Name", SqlDbType.VarChar).Value = (string)transaction["name"];
                                    cmd.Parameters.Add("@Location_State", SqlDbType.VarChar).Value = (string)transaction["location"]["state"];
                                    cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = (decimal)transaction["amount"];

                                    await conn.OpenAsync();
                                    await cmd.ExecuteNonQueryAsync();
                                    saved++;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    offset += 500;
                }
                string outputmessage = String.Format("Saved {0} transactions to the database.", saved);
                log.Info(outputmessage);

                return req.CreateResponse(HttpStatusCode.OK);
            }
        }
    }
}
