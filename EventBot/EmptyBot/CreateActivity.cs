using EventBot.Models;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot
{
    public class CreateActivity
    {
        public static string GetTextForNoEventsFound(EventParams eventParams)
        {
            string replyText;
            replyText = "I didn't find events";
            if (!string.IsNullOrWhiteSpace(eventParams.Genre))
            {
                replyText += $" with genre {eventParams.Genre}";
            }
            if (eventParams.Radius != 0)
            {
                replyText += $" not further than {eventParams.Radius}km from your location";
            }
            if (!string.IsNullOrWhiteSpace(eventParams.City))
            {
                if (eventParams.City == "nearby")
                {
                    //replyText += $" {eventParams.City}";
                }
                else
                {
                    replyText += $" at {eventParams.City}";
                }
            }
            if (!string.IsNullOrWhiteSpace(eventParams.StartDate))
            {
                if (eventParams.StartDate == eventParams.EndDate)
                {
                    replyText += $" on {eventParams.StartDate}";
                }
                else
                {
                    replyText += $" between {eventParams.StartDate} and {eventParams.EndDate}";
                }
            }
            replyText += ".";
            return replyText;
        }

        public static List<Attachment> GetAttachementForFoundEvents(List<Event> events)
        {
            List<Attachment> attachments = new List<Attachment>();
            foreach (var eventObject in events)
            {
                HeroCard heroCard = new HeroCard();
                List<CardImage> cardImages = new List<CardImage>()
                {
                    new CardImage() {
                        Url = eventObject.Images[0].Url
                    }
                };
                heroCard.Images = cardImages;
                heroCard.Title = eventObject.Name;
                if (eventObject.Dates.Start.DateTime == DateTime.MinValue)
                {
                    heroCard.Subtitle = eventObject.Dates.Start.LocalDate.ToString();
                }
                else
                {
                    heroCard.Subtitle = eventObject.Dates.Start.DateTime.ToString();
                }
                if (eventObject.PriceRanges != null)
                {
                    heroCard.Text = $"\nPrice is between {eventObject.PriceRanges[0].Min} - {eventObject.PriceRanges[0].Max} {eventObject.PriceRanges[0].Currency}";
                }
                if (eventObject._Embedded.Venues[0].City.Name != null)
                {
                    heroCard.Text += $"\n{eventObject._Embedded.Venues[0].City.Name}";
                }
                if (eventObject.Distance != 0)
                {
                    heroCard.Text += $"\nDistance: {eventObject.Distance}km";
                }
                List<CardAction> cardActions = new List<CardAction>()
                {
                    new CardAction
                    {
                        Type = "openUrl",
                        Value = eventObject.Url,
                        Title = "Book tickets"
                    }
                };
                heroCard.Buttons = cardActions;
                attachments.Add(heroCard.ToAttachment());
            }
            return attachments;
        }

        public static string GetTextForFoundEvents(EventParams eventParams)
        {
            //return $"This is what I found for events with genre {eventParams.Genre} not further than {eventParams.Radius}km from {eventParams.City} on {eventParams.Date.ToString()}:";
            string replyText;
            replyText = "This is what I found for events";
            if (!string.IsNullOrWhiteSpace(eventParams.Genre))
            {
                replyText += $" with genre {eventParams.Genre}";
            }
            if (eventParams.Radius != 0)
            {
                replyText += $" not further than {eventParams.Radius}km from your location";
            }
            if (!string.IsNullOrWhiteSpace(eventParams.City))
            {
                if (eventParams.City == "nearby")
                {
                    //replyText += $" {eventParams.City}";
                }
                else
                {
                    replyText += $" at {eventParams.City}";
                }
            }
            if (!string.IsNullOrWhiteSpace(eventParams.StartDate))
            {
                if (eventParams.StartDate == eventParams.EndDate)
                {
                    replyText += $" on {eventParams.StartDate}";
                }
                else
                {
                    replyText += $" between {eventParams.StartDate} and {eventParams.EndDate}";
                }
            }
            replyText += ".";
            return replyText;
        }

        public static SuggestedActions GetSuggestedActionsForGenres(List<Segment> segments)
        {
            List<CardAction> actions = new List<CardAction>()
            {
                new CardAction() { Title = "None", Type = ActionTypes.ImBack, Value = "None" }
            };
            foreach (var segment in segments)
            {
                actions.Add(new CardAction() { Title = segment.Name, Type = ActionTypes.ImBack, Value = segment.Name });
            }

            SuggestedActions suggestedActions = new SuggestedActions() { Actions = actions };
            return suggestedActions;
        }

        public static SuggestedActions CreateSuggestedAction(List<string> values)
        {
            List<CardAction> actions = new List<CardAction>();
            foreach (var value in values)
            {
                actions.Add(new CardAction() { Title = value, Type = ActionTypes.ImBack, Value = value });
            }
            SuggestedActions suggestedAction = new SuggestedActions() { Actions = actions };
            return suggestedAction;
        }
    }
}
