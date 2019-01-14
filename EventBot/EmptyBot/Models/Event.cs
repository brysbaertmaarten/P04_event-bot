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
