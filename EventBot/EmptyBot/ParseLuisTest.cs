using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventBot
{
    public class ParseLuisTest
    {
        public static DateTime ParseDateEntitie(string entitie)
        {
            var culture = Culture.English;
            var d = DateTimeRecognizer.RecognizeDateTime(entitie, culture);

            var first = d.First();
            var subType = first.TypeName.Split('.').Last();
            var resolutionValues = (IList<Dictionary<string, string>>)first.Resolution["values"];
            if (subType.Contains("date") && !subType.Contains("range"))
            {
                // a date (or date & time) or multiple
                var moment = resolutionValues.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();

                return moment;

            }
            else if (subType.Contains("date") && subType.Contains("range"))
            {
                // range
                var from = DateTime.Parse(resolutionValues.First()["start"]);
                var to = DateTime.Parse(resolutionValues.First()["end"]);

                return from;
            }
            else
            {
                return DateTime.Now;
            }
        }        
    }
}
