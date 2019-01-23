using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot.Models
{
    public class RootObject
    {
        [JsonProperty(propertyName: "_embedded")]
        public Embedded Embedded { get; set; }
    }
        public class Embedded
        {
            [JsonProperty(propertyName: "events")]
            public List<Event> Events { get; set; }
        }

            public class Event
            {
                [JsonProperty(propertyName: "name")]
                public string Name { get; set; }
                [JsonProperty(propertyName: "images")]
                public List<Image> Images { get; set; }
                [JsonProperty(propertyName: "classifications")]
                public List<Classification> Classifications { get; set; }
                [JsonProperty(propertyName: "dates")]
                public Dates Dates { get; set; }
                [JsonProperty(propertyName: "url")]
                public string Url { get; set; }
                [JsonProperty(propertyName: "priceRanges")]
                public List<PriceRange> PriceRanges { get; set; }
                [JsonProperty(propertyName: "distance")]
                public double Distance { get; set; }
                [JsonProperty(propertyName: "_embedded")]
                public _Embedded _Embedded { get; set; }
            }
                public class _Embedded
                {
                    [JsonProperty(propertyName: "venues")]
                    public List<Venue> Venues { get; set; }
                }
                    public class Venue
                    {
                        [JsonProperty(propertyName: "city")]
                        public City City { get; set; }
                    }
                        public class City
                        {
                            [JsonProperty(propertyName: "name")]
                            public string Name { get; set; }
                        }

                public class Image
                {
                    [JsonProperty(propertyName: "url")]
                    public string Url { get; set; }
                    [JsonProperty(propertyName: "width")]
                    public int Width { get; set; }
                    [JsonProperty(propertyName: "height")]
                    public int Height { get; set; }
                }

                public class PriceRange
                {
                    [JsonProperty(propertyName: "currency")]
                    public string Currency { get; set; }
                    [JsonProperty(propertyName: "min")]
                    public double Min { get; set; }
                    [JsonProperty(propertyName: "max")]
                    public double Max { get; set; }
                }

                public class Dates
                {
                    [JsonProperty(propertyName: "start")]
                    public Start Start { get; set; }
                }
                    public class Start
                    {
                        [JsonProperty(propertyName: "dateTime")]
                        public DateTime DateTime { get; set; }
                        [JsonProperty(propertyName: "localDate")]
                        public DateTime LocalDate { get; set; }
                    }

                public class Classification
                {
                    [JsonProperty(propertyName: "segment")]
                    public Segment Segment { get; set; }
                }
                    public class Segment
                    {
                        [JsonProperty(propertyName: "name")]
                        public string Name { get; set; }
                    }
                
}
