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

namespace PlaidWebhook
{
    public static class Delete_Transaction
    {
        [FunctionName("Delete_Transaction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        { 
            var contents = req.Content.ReadAsStringAsync().Result;
            var itemdata = JObject.Parse(contents);
            var str = ConfigurationManager.ConnectionStrings["sqlConnection"].ConnectionString;
            
            var transaction_id_list = itemdata["removed_transactions"];

            foreach (var transaction_id in transaction_id_list)
            {
                using (SqlConnection conn = new SqlConnection(str))
                {
                    conn.Open();
                    var text = "DELETE FROM dbo.User_Transactions where TransactionID like '" + (string)transaction_id + "'";

                    using (SqlCommand cmd = new SqlCommand(text, conn))
                    {
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    }
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
