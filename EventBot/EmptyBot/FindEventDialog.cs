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
            _dialogSet.Add(new DateTimePrompt(EventDatePrompt, DateValidatorAsync));

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
                stepContext.Values["date"] = date;
                return await stepContext.NextAsync();
            }
            else
            {
                return await stepContext.PromptAsync(
                   EventDatePrompt,
                   new PromptOptions
                   {
                       Prompt = MessageFactory.Text("When must the event find place?"),
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
                stepContext.Values["date"] = date;
            }
            if (city != null)
            {
                stepContext.Values["city"] = city;
                return await stepContext.NextAsync();
            }

            return await stepContext.PromptAsync(
                LocationPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Where must the event find place?"),
                    RetryPrompt = MessageFactory.Text("Where?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForRadiusAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var city = stepContext.Result;
            if (city != null)
            {
                stepContext.Values["city"] = city;
            }
            else
            {
                city = EventBot.eventParams.City;
            }

            return await stepContext.PromptAsync(
                RadiusPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"What's the maximum distance in Km from {city}?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForGenreAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var radius = stepContext.Result;
            stepContext.Values["radius"] = radius;

            List<string> genres = new List<string>()
            {
                "Theatre",
                "Football",
                "Music"
            };

            var reply = stepContext.Context.Activity.CreateReply("Which genre of events are you  looking for?");
            List<CardAction> actions = new List<CardAction>()
            {
                new CardAction() { Title = "None", Type = ActionTypes.ImBack, Value = "None" }
            };

            foreach (var genre in genres)
            {
                actions.Add(new CardAction() { Title = genre, Type = ActionTypes.ImBack, Value = genre });
            }
            reply.SuggestedActions = new SuggestedActions() { Actions = actions };

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
            var genre = stepContext.Result;
            stepContext.Values["genre"] = genre;

            EventParams eventParams = new EventParams
            {
                Date = (string)stepContext.Values["date"],
                Radius = (float)stepContext.Values["radius"],
                Genre = genre.ToString(),
                City = (string)stepContext.Values["city"],
            };

            // Return the collected information to the parent context.
            return await stepContext.EndDialogAsync(eventParams, cancellationToken);
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
