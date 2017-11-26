using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace PlaidWebhook
{
    public static class WebhookEntry
    {
        [FunctionName("WebhookEntry")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            HttpClient client = new HttpClient();

            // Get the webhook code object from the JSON in the message body.
            var contents = req.Content.ReadAsStringAsync().Result;
            var webhookdata = JObject.Parse(contents);

            //parse Webhook JSON for the webhook code and pass item ID to next function
            string webhook_code = (string)webhookdata["webhook_code"];
            string item_id = (string)webhookdata["item_id"];

            //log what type of webhook was received.
            log.Info(String.Format("{0} webhook received", webhook_code));

            //Transactions that were received that are to be removed from the DB
            if (webhook_code.Equals("TRANSACTIONS_REMOVED"))
            {
                List<string> removed_transactions = new List<string>();

                foreach (var transaction in webhookdata["removed_transactions"])
                {
                    removed_transactions.Add(transaction.ToString());
                }

                string serialized_removed_transactions = String.Format("{{ \"item_id\":\"{0}\" , \"removed_transactions\":\"{1}\" }}", item_id, removed_transactions);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_Delete_Transaction?code=lgxmkyJxey5JhGefm7W/yVUpCJi/nlwoQxGLYna2yFZeAZKgckdJfg==");
                request.Content = new StringContent(serialized_removed_transactions);

                await client.SendAsync(request);
                return req.CreateResponse(HttpStatusCode.OK);
            }

            int new_transactions = (int)webhookdata["new_transactions"];
            log.Info(String.Format("{0} transactions received.", new_transactions));

            string data = String.Format("{{ \"item_id\":\"{0}\" , \"new_transactions\":\"{1}\" }}", item_id, new_transactions);

            if (webhook_code.Equals("INITIAL_UPDATE"))
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_Initial?code=FWVmlFolIZB4qurzK4KSz76lExltcfgabsfdmr5zx0lSfp5/oY/Z1w==")
                {
                    Content = new StringContent(data)
                };

                await client.SendAsync(request);
                return req.CreateResponse(HttpStatusCode.OK);
            }

            if (webhook_code.Equals("HISTORICAL_UPDATE"))
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_Historical?code=SL8CipVodzWqdomcuMNsud7Fbe3Xne/0FZEXBo1/sAV4HvaNUabT9Q==");

                request.Content = new StringContent(data);

                await client.SendAsync(request);
                return req.CreateResponse(HttpStatusCode.OK);
            }

            else if (webhook_code.Equals("DEFAULT_UPDATE"))
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://dhwebhookentry.azurewebsites.net/api/Plaid_NewTransactions?code=8PWvoPeWUoWsRyWPys3xXaE8//fp3FNEvzhsk0saLdLLhIfK6wNFgg==");

                request.Content = new StringContent(data);

                await client.SendAsync(request);
                return req.CreateResponse(HttpStatusCode.OK);
            }

            //If we got this far, something went wrong.
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}