using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot.Models
{
    public class FacebookChannelData
    {
        public class ChannelData
        {
            public Message Message { get; set; }
        }
        public class Message
        {
            public List<Attachment> Attachments { get; set; }
        }
        public class Attachment
        {
            public string Type { get; set; }
            public Payload Payload { get; set; }
        }
        public class Payload
        {
            public Coordinates Coordinates { get; set; }
        }
        public class Coordinates
        {
            public double Lat { get; set; }
            public double Long { get; set; }
        }

    }
}
