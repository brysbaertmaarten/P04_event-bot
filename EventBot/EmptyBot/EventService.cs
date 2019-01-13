using EventBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EventBot
{
    public class EventService
    {
        static HttpClient client = new HttpClient();
        private const string apiKey = "dyXmi09sDm4XGbrxHw14yCkA5E43Ok9R";
        private const string baseUrl = "https://app.ticketmaster.com/discovery/v2/events.json?";

        private static string city;
        private static string latLong;
        private static string classification;
        private static string radius;
        private static string date;

        public static string CreateUrl(EventParams eventParams)
        {
            string url = baseUrl;
            if (!string.IsNullOrWhiteSpace(eventParams.City))
            {
                url += $"&city={eventParams.City}";
            }
            url += $"&apikey={apiKey}";
            return url;
        }

        public static async Task<List<Event>> GetEventsAsync(EventParams eventParams)
        {
            string url = CreateUrl(eventParams);
            List<Event> result = new List<Event>();

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                string test = content;
                result = JsonConvert.DeserializeObject<List<Event>>(content);
            }
            return result;
        }
    }
}
