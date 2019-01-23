using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventBot
{
    public class ParseLuis
    {
        // extract entities from recognizerResult
        public static EventParams GetEntities(RecognizerResult recognizerResult)
        {
            var entities = recognizerResult.Entities;
            var date = entities["datetime"];
            var instance = entities["$instance"];
            var location = entities["Places_AbsoluteLocation"];
            var city = entities["geographyV2_city"];
            EventParams eventParams = new EventParams();

            if (date != null)
            {
                var text = instance["datetime"][0]["text"].ToString();
                try
                {
                    List<DateTime> dates = ParseDateEntitie(text);
                    eventParams.StartDate = dates[0].ToString();
                    eventParams.EndDate = dates[1].ToString();
                }
                catch (Exception)
                {

                }
            }
            if (location != null)
            {
                string loc = location[0].ToString();
                eventParams.City = loc;
            }
            if (city != null)
            {
                eventParams.City = city[0].ToString();
            }
            return eventParams;
        }

        public static List<DateTime> ParseDateEntitie(string entitie)
        {
            var culture = Culture.English;
            var d = DateTimeRecognizer.RecognizeDateTime(entitie, culture);

            List<DateTime> dates = new List<DateTime>();

            var first = d.First();
            var subType = first.TypeName.Split('.').Last();
            var resolutionValues = (IList<Dictionary<string, string>>)first.Resolution["values"];
            if (subType.Contains("date") && !subType.Contains("range"))
            {
                // a date (or date & time) or multiple
                var moment = resolutionValues.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();
                dates.Add(moment); // start
                dates.Add(moment); // end
            }
            else if (subType.Contains("date") && subType.Contains("range"))
            {
                // range
                dates.Add(DateTime.Parse(resolutionValues.First()["start"]));
                dates.Add(DateTime.Parse(resolutionValues.First()["end"]));
            }
            return dates;
        }        
    }
}
