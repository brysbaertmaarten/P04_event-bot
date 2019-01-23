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
        //private const string XMashapeKey = "iV1jeW0Ls1mshZd4Vdxn6axLeeCOp1arzd8jsng2EKayNqE4Zi";
        private const string baseUrl = "https://icanhazdadjoke.com/";

        public static async Task<string> GetJoke()
        {
            string result = "";
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(baseUrl),
                Headers = {
                    { "Accept", "application/json" }
                }
            };
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(content);
                result = obj["joke"];
            }
            return result;
        }
    }
}
