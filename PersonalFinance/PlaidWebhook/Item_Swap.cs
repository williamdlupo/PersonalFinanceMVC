using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data.SqlClient;
using System;
using Newtonsoft.Json;
using System.Text;

namespace PlaidWebhook
{
    public static class Item_Swap
    {
        [FunctionName("Item_Swap")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            string _access_token = "";

            var contents = req.Content.ReadAsStringAsync().Result;

            var itemdata = JObject.Parse(contents);

            //parse Webhook JSON for the webhook code and pass item ID to next function
            string item_id = (string)itemdata["item_id"];

            //Get the access token from the DB that correlates to the item ID
            var str = ConfigurationManager.ConnectionStrings["sqlConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                var text = "select Access_Token from dbo.User_Items where Item_ID like '" + item_id + "'";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        _access_token = (string)reader[0];
                    }
                }
            }

            //Send Access Token 
            var myObj = new { access_token = _access_token };
            var jsonToReturn = JsonConvert.SerializeObject(myObj);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonToReturn, Encoding.UTF8, "application/json")
            };
        }
    }
}
