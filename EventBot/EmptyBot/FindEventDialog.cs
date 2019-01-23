using EventBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static EventBot.Models.FacebookChannelData;

namespace EventBot
{
    public class FindEventDialog : DialogSet
    {
        private const string Dialog = "findEventDialog";
        public static readonly string LuisKey = "EventBot";

        private const string GenrePrompt = "genrePrompt";
        private const string LocationPrompt = "locationPrompt";
        private const string EventDatePrompt = "datePrompt";
        private const string RadiusPrompt = "radiusPrompt";

        private readonly EventBotAccessors _accessors;
        private readonly EventService eventService;
        private readonly BotServices services;

        // The following code creates prompts and adds them to an existing dialog set. The DialogSet contains all the dialogs that can 
        // be used at runtime. The prompts also references a validation method is not shown here.
        public FindEventDialog(EventBotAccessors accessors, EventService eventService, BotServices services) : base(accessors.DialogState)
        {
            _accessors = accessors;
            this.eventService = eventService;
            this.services = services;

            // add prompts to dialog
            Add(new TextPrompt(GenrePrompt));
            Add(new TextPrompt(LocationPrompt));
            Add(new NumberPrompt<float>(RadiusPrompt));
            Add(new DateTimePrompt(EventDatePrompt));

            // Define the steps of the waterfall dialog and add it to the set.
            WaterfallStep[] steps = new WaterfallStep[]
            {
                PromptForDateAsync,
                PromptForLocationAsync,
                PromptForRadiusAsync,
                PromptForGenreAsync,
                ReturnEventsAsync,
            };
            Add(new WaterfallDialog(Dialog, steps));

        }

        private async Task<DialogTurnResult> PromptForDateAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {

            EventParams eventParams = await _accessors.EventParamState.GetAsync(stepContext.Context);
            string date = eventParams.StartDate;
            if (date != null)
            {
                return await stepContext.NextAsync();
            }
            else
            {
                return await stepContext.PromptAsync(
                   EventDatePrompt,
                   new PromptOptions
                   {
                       Prompt = MessageFactory.Text("When must the event take place?"),
                       RetryPrompt = MessageFactory.Text("Please enter a valid time description like 'today', 'this weekend' or '10th of March'. \nEnd this search by typing 'end'"),
                   },
                   cancellationToken
               );
            }
        }

        private async Task<DialogTurnResult> PromptForLocationAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            EventParams eventParams = await _accessors.EventParamState.GetAsync(stepContext.Context);
            string city = eventParams.City;
            if (stepContext.Result != null)
            {
                DateTimeResolution resolution = (stepContext.Result as IList<DateTimeResolution>).First();
                if (resolution.Value != null)
                {
                    eventParams.StartDate = resolution.Value;
                    eventParams.EndDate = resolution.Value;
                }
                else
                {
                    eventParams.StartDate = resolution.Start;
                    eventParams.EndDate = resolution.End;
                }
            }
            await _accessors.EventParamState.SetAsync(stepContext.Context, eventParams);
            if (city != null)
            {
                return await stepContext.NextAsync();
            }

            var reply = stepContext.Context.Activity.CreateReply();
            if (stepContext.Context.Activity.ChannelId == Microsoft.Bot.Connector.Channels.Facebook)
            {
                var channelData = JObject.FromObject(new { quick_replies = new dynamic[] { new { content_type = "location" } } });
                reply.ChannelData = channelData;
                reply.Text = "Give up a city or send your location to find events nearby.";
            }
            else
            {
                reply.Text = "Give up a city where the event should take place.";
            }

            return await stepContext.PromptAsync(
                LocationPrompt,
                new PromptOptions
                {
                    Prompt = reply,
                    RetryPrompt = MessageFactory.Text("I did not manage to process your location. Please give up a city."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForRadiusAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            EventParams eventParams = await _accessors.EventParamState.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
            {
                if (stepContext.Context.Activity.ChannelId == Microsoft.Bot.Connector.Channels.Facebook)
                {
                    ChannelData channelData = ChannelHelper.GetChannel(stepContext.Context);
                    if (channelData.Message != null)
                    {
                        string loc = ChannelHelper.GetGeoHash(channelData);
                        if (loc != null)
                        {
                            eventParams.GeoHash = loc;
                        }
                    }
                }
                if (stepContext.Context.Activity.Text != null)
                {
                    var recognizerResult = await services.LuisServices[LuisKey].RecognizeAsync(stepContext.Context, cancellationToken);
                    eventParams.City = ParseLuis.GetEntities(recognizerResult).City;
                    if (eventParams.City == null)
                    {
                        eventParams.City = stepContext.Result.ToString();
                    }
                }
            }
            await _accessors.EventParamState.SetAsync(stepContext.Context, eventParams);
            if (eventParams.GeoHash == null || eventParams.Radius > 0)
            {
                return await stepContext.NextAsync();
            }

            return await stepContext.PromptAsync(
                RadiusPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"What's the maximum distance in Km from your location?"),
                    RetryPrompt = MessageFactory.Text($"Please enter a number like '30'. \nEnd this search by typing 'end'"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForGenreAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            EventParams eventParams = await _accessors.EventParamState.GetAsync(stepContext.Context);
            if (eventParams.Genre != null)
            {
                return await stepContext.NextAsync();
            }
            if (stepContext.Result != null)
            {
                eventParams.Radius = (float)stepContext.Result;
                await _accessors.EventParamState.SetAsync(stepContext.Context, eventParams);
            }

            // bij het wachten toont de bot een "typing" teken
            var typing = stepContext.Context.Activity.CreateReply();
            typing.Type = ActivityTypes.Typing;
            await stepContext.Context.SendActivityAsync(typing);

            // vraag segmenten (=genres) op, op basis van ingegeven parameters
            List<Segment> segments = await eventService.GetSegmentsAsync(eventParams);
            // indien geen segmenten (=genres), er zijn geen evenementen gevonden voor deze parameters.
            if (segments.Count == 0)
            {
                return await stepContext.EndDialogAsync(cancellationToken);
            }

            var reply = stepContext.Context.Activity.CreateReply("Which genre of events are you looking for?");
            reply.SuggestedActions = CreateActivity.GetSuggestedActionsForGenres(segments);

            return await stepContext.PromptAsync(
                GenrePrompt,
                new PromptOptions
                {
                    Prompt = reply,
                    RetryPrompt = MessageFactory.Text("Describe a genre please."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ReturnEventsAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            EventParams eventParams = await _accessors.EventParamState.GetAsync(stepContext.Context);
            if (stepContext.Result != null)
            {
                eventParams.Genre = (string)stepContext.Result;
                await _accessors.EventParamState.SetAsync(stepContext.Context, eventParams);
            }

            return await stepContext.EndDialogAsync(cancellationToken);
        }

        private async Task<bool> DateValidatorAsync(
            PromptValidatorContext<IList<DateTimeResolution>> promptContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                "Please enter a date of time for your event.",
                cancellationToken: cancellationToken);
                return false;
            }

            DateTimeResolution value = promptContext.Recognized.Value.FirstOrDefault();
            return true;
        }

        private async Task<bool> LocationValidatorAsync(
            PromptValidatorContext<Activity> promptContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                "Please enter a date of time for your event.",
                cancellationToken: cancellationToken);
                return false;
            }



            return true;
        }
    }
}
