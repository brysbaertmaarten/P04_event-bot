using EventBot.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private static string CreateUrl(EventParams eventParams)
        {
            string url = baseUrl;
            if (!string.IsNullOrWhiteSpace(eventParams.City))
            {
                url += $"&city={eventParams.City}";
            }
            if (true)
            {
                url += $"&radius={eventParams.Radius}";
            }
            if (!string.IsNullOrWhiteSpace(eventParams.Genre) && eventParams.Genre.ToLower() != "none")
            {
                url += $"&classificationName={eventParams.Genre}";
            }
            if (!string.IsNullOrWhiteSpace(eventParams.Date))
            {
                string date = eventParams.Date;
                DateTime d = Convert.ToDateTime(date);
                date = d.ToString("yyyy-MM-ddTHH:mm:ss");

                url += $"&localStartDateTime={date},*";
            }

            url += "&unit=km";
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
                RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(content);
                if (rootObject.Embedded != null)
                {
                    result = rootObject.Embedded.Events;
                }
            }
            return result;
        }

        public static async Task<List<Segment>> GetSegmentsAsync(EventParams eventParams)
        {
            List<Segment> segments = new List<Segment>();
            List<string> segmentStrings = new List<string>();
            List<Event> events = await GetEventsAsync(eventParams);
            foreach (var eventObject in events)
            {
                var segment = eventObject.Classifications[0].Segment;
                if (!segmentStrings.Contains(segment.Name))
                {
                    segments.Add(segment);
                    segmentStrings.Add(segment.Name);
                }
            }
            return segments;
        }
    }
}
