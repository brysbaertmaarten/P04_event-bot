using EventBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventBot
{
    public class FindEventDialog
    {
        private const string Dialog = "findEventDialog";

        private const string GenrePrompt = "genrePrompt";
        private const string LocationPrompt = "locationPrompt";
        private const string EventDatePrompt = "datePrompt";
        private const string RadiusPrompt = "radiusPrompt";

        public readonly DialogSet _dialogSet;
        private readonly EventBotAccessors _accessors;
        private readonly ILogger _logger;

        // The following code creates prompts and adds them to an existing dialog set. The DialogSet contains all the dialogs that can 
        // be used at runtime. The prompts also references a validation method is not shown here.
        public FindEventDialog(EventBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<FindEventDialog>();
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            // Create the dialog set and add the prompts, including custom validation.
            _dialogSet = new DialogSet(_accessors.DialogStateAccessor);
            _dialogSet.Add(new TextPrompt(GenrePrompt));
            _dialogSet.Add(new TextPrompt(LocationPrompt));
            _dialogSet.Add(new NumberPrompt<float>(RadiusPrompt));
            _dialogSet.Add(new DateTimePrompt(EventDatePrompt));

            // Define the steps of the waterfall dialog and add it to the set.
            WaterfallStep[] steps = new WaterfallStep[]
            {
                PromptForDateAsync,
                PromptForLocationAsync,
                PromptForRadiusAsync,
                PromptForGenreAsync,
                ReturnEventsAsync,
            };
            _dialogSet.Add(new WaterfallDialog(Dialog, steps));
        }

        private async Task<DialogTurnResult> PromptForDateAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string date = EventBot.eventParams.Date;
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
                       RetryPrompt = MessageFactory.Text("Please enter a valid time description."),
                   },
                   cancellationToken
               );
            }
        }

        private async Task<DialogTurnResult> PromptForLocationAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string city = EventBot.eventParams.City;
            if (stepContext.Result != null)
            {
                DateTimeResolution resolution = (stepContext.Result as IList<DateTimeResolution>).First();
                string date = resolution.Value ?? resolution.Start;
                EventBot.eventParams.Date = date;
            }
            if (city != null)
            {
                return await stepContext.NextAsync();
            }

            return await stepContext.PromptAsync(
                LocationPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Which city should the event take place at?"),
                    RetryPrompt = MessageFactory.Text("Give up a city please."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForRadiusAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stepContext.Result != null)
            {
                EventBot.eventParams.City = stepContext.Result.ToString();
            }
            return await stepContext.NextAsync(); // tijdelijk (radius alleen opvragen bij gebruik locatie vd gebruiker)

            return await stepContext.PromptAsync(
                RadiusPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"What's the maximum distance in Km from your location?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForGenreAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (EventBot.eventParams.Genre != null)
            {
                return await stepContext.NextAsync();
            }
            if (stepContext.Result != null)
            {
                EventBot.eventParams.Radius = (float)stepContext.Result;
            }

            // vraag segmenten (=genres) op, op basis van ingegeven parameters
            List<Segment> segments = await EventService.GetSegmentsAsync(EventBot.eventParams);
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
            if (stepContext.Result != null)
            {
                EventBot.eventParams.Genre = (string)stepContext.Result;
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
    }
}
