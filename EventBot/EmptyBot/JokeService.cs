using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EventBot
{
    public class JokeService
    {
        static HttpClient client = new HttpClient();
        private const string XMashapeKey = "iV1jeW0Ls1mshZd4Vdxn6axLeeCOp1arzd8jsng2EKayNqE4Zi";
        private const string baseUrl = "https://webknox-jokes.p.mashape.com/jokes/oneLiner";

        public static async Task<string> GetJoke()
        {
            string result = "";
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(baseUrl),
                Headers = {
                    { "X-Mashape-Key", "iV1jeW0Ls1mshZd4Vdxn6axLeeCOp1arzd8jsng2EKayNqE4Zi" }
                }
            };
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(content);
                result = obj["text"];
            }
            return result;
        }
    }
}
