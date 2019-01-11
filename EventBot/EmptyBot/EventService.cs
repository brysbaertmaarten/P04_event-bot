using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot
{
    public class EventService
    {
        private const string apiKey = "dyXmi09sDm4XGbrxHw14yCkA5E43Ok9R";
        private const string baseUrl = "https://app.ticketmaster.com/discovery/v2/events.json?";

        private static string city;
        private static string latLong;
        private static string classification;
        private static string radius;
        private static string date;
    }
}
