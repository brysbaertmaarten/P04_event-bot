using EventBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGeoHash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EventBot.Models.FacebookChannelData;

namespace EventBot
{
    public class ChannelHelper
    {
        public static ChannelData GetChannel(ITurnContext turnContext)
        {
            string channelDataString = turnContext.Activity.ChannelData.ToString();
            //string channelDataString = turnContext;
            ChannelData channelData = JsonConvert.DeserializeObject<ChannelData>(channelDataString);
            return channelData;
        }

        public static string GetLocation(ChannelData channelData)
        {
            if (channelData.Message != null && channelData.Message.Attachments != null)
            {
                foreach (var attachment in channelData.Message.Attachments)
                {
                    if (attachment.Type == "location")
                    {
                        FacebookChannelData.Coordinates coordinates = attachment.Payload.Coordinates;
                        string coordinatesString = coordinates.Lat + ", " + coordinates.Long;
                        return coordinatesString;
                    }
                }
            }
            return null;
        }

        public static string GetGeoHash(ChannelData channelData)
        {
            if (channelData.Message != null && channelData.Message.Attachments != null)
            {
                foreach (var attachment in channelData.Message.Attachments)
                {
                    if (attachment.Type == "location")
                    {
                        FacebookChannelData.Coordinates coordinates = attachment.Payload.Coordinates;
                        string geoHash = GeoHash.Encode(coordinates.Lat, coordinates.Long);
                        return geoHash;
                    }
                }
            }
            return null;
        }
    }
}
