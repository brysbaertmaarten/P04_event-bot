// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;

namespace EventBot
{
    public class EventBot : IBot
    {
        public static readonly string LuisKey = "EventBot";

        private readonly EventBotAccessors accessors;
        private readonly FindEventDialog findEventDialog;
        private readonly BotServices services;

        public static EventParams eventParams = new EventParams();
        public static int pageCount = 0;

        DialogTurnResult dialogTurnResult = new DialogTurnResult(DialogTurnStatus.Empty);

        public EventBot(EventBotAccessors accessors, FindEventDialog findEventDialog, BotServices services)
        {
            this.accessors = accessors;
            this.findEventDialog = findEventDialog;
            this.services = services ?? throw new System.ArgumentNullException(nameof(services));
            if (!services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration....");
            }
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // input doorsturen naar LUIS API
                var recognizerResult = await services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                //var recognizerResult = await services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                var intent = recognizerResult?.GetTopScoringIntent().intent;

                // Generate a dialog context for our dialog set.
                DialogContext dc = await findEventDialog._dialogSet.CreateContextAsync(turnContext, cancellationToken);

                // Als er geen dialoog bezig is
                if (dc.ActiveDialog is null)
                {
                    switch (intent)
                    {
                        case "FindEventIntent":
                            // Check if there are entities recognized (moet voor het begin van het dialog)
                            eventParams = ParseLuis.GetEntities(recognizerResult); pageCount = 0;
                            // start new dialog 
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "GreetingIntent":
                            var greetingReply = turnContext.Activity.CreateReply();
                            greetingReply.Text = MessageService.GetMessage("GreetingAnswer"); // geeft een random antwoord van de greetingAnswer list terug
                            greetingReply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { "Find me an event!" });
                            await turnContext.SendActivityAsync(greetingReply);
                            break;
                        case "ThankIntent":
                            string thankReply = MessageService.GetMessage("ThankAnswer");
                            await turnContext.SendActivityAsync(thankReply);
                            break;
                        case "ChangeDateIntent":
                            eventParams.Date = null; pageCount = 0;
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "ChangeCityIntent":
                            eventParams.City = ParseLuis.GetEntities(recognizerResult).City;
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "ChangeGenreIntent":
                            eventParams.Genre = null; pageCount = 0;
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "MoreEventsIntent":
                            pageCount += 1;
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "None":
                            break;
                    }
                }
                else
                {
                    // Continue the dialog.
                    switch (intent)
                    {
                        case "QuitIntent":
                            dialogTurnResult = await dc.CancelAllDialogsAsync(cancellationToken);
                            break;
                        default:
                            dialogTurnResult = await dc.ContinueDialogAsync(cancellationToken);
                            break;
                    }
                }

                // If the dialog completed this turn, doe iets met de eventParams
                if (dialogTurnResult.Status is DialogTurnStatus.Complete)
                {
                    List<Event> events = await EventService.GetEventsAsync(eventParams, pageCount);
                    var reply = turnContext.Activity.CreateReply();

                    if (events.Count() != 0)
                    {
                        reply.Text = CreateActivity.GetTextForFoundEvents(eventParams);
                        reply.Attachments = CreateActivity.GetAttachementForFoundEvents(events);
                        reply.AttachmentLayout = "carousel";
                        reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { "more", "change genre", "change city" });
                    }
                    else
                    {
                        reply.Text = CreateActivity.GetTextForNoEventsFound(eventParams);
                        if (eventParams.Genre == null)
                        {
                            reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { "change city" });
                        }
                        else
                        {
                            reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { "change genre", "change city" });
                        }
                    }

                    // send reply
                    await turnContext.SendActivityAsync(reply);
                }

                // Save the updated dialog state into the conversation state.
                await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
        }
    }
}
