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

        private static string CreateUrl(EventParams eventParams, int page)
        {
            string url = baseUrl;
            if (!string.IsNullOrWhiteSpace(eventParams.City) && eventParams.City != "nearby")
            {
                url += $"&city={eventParams.City}";
            }
            if (eventParams.GeoHash != null)
            {
                url += $"&geoPoint={eventParams.GeoHash}";
            }
            if (eventParams.Radius > 0)
            {
                url += $"&radius={eventParams.Radius}";
            }
            if (!string.IsNullOrWhiteSpace(eventParams.Genre) && eventParams.Genre.ToLower() != "none")
            {
                url += $"&classificationName={eventParams.Genre}";
            }
            if (!string.IsNullOrWhiteSpace(eventParams.StartDate) && !string.IsNullOrWhiteSpace(eventParams.EndDate))
            {
                string startDate = eventParams.StartDate;
                DateTime sd = Convert.ToDateTime(startDate);

                string endDate = eventParams.EndDate;
                DateTime ed = Convert.ToDateTime(endDate);

                // als startdatum overeenkomt met einddatum, tel 1 dag bij de einddatum. Anders wordt er bv
                // een evenement gezocht tussen 17/01/2019 0:00:00 en 17/01/2019 0:00:00 wat sws uitdraaid op geen resultaten
                if (sd == ed)
                {
                    ed = ed.AddDays(1);
                }

                startDate = sd.ToString("yyyy-MM-ddTHH:mm:ss");
                endDate = ed.ToString("yyyy-MM-ddTHH:mm:ss");

                url += $"&localStartDateTime={startDate},{endDate}";
            }

            url += $"&page={page}";
            url += "&size=10";
            url += "&unit=km";
            url += $"&apikey={apiKey}";
            return url;
        }

        public async Task<List<Event>> GetEventsAsync(EventParams eventParams, int page)
        {
            string url = CreateUrl(eventParams, page);
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

        public async Task<List<Segment>> GetSegmentsAsync(EventParams eventParams)
        {
            int page = 0;
            List<Segment> segments = new List<Segment>();
            List<string> segmentStrings = new List<string>();
            while (page < 5)
            {
                List<Event> events = await GetEventsAsync(eventParams, page);
                foreach (var eventObject in events)
                {
                    var segment = eventObject.Classifications[0].Segment;
                    if (!segmentStrings.Contains(segment.Name))
                    {
                        segments.Add(segment);
                        segmentStrings.Add(segment.Name);
                    }
                }
                page += 1;
            }
            return segments;
        }
    }
}
