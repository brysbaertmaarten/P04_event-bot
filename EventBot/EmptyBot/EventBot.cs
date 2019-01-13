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
                var entities = recognizerResult.Entities;
                var date = entities["datetime"];
                var instance = entities["$instance"];
                var location = entities["Places_AbsoluteLocation"];
                var intent = recognizerResult?.GetTopScoringIntent().intent;

                // service TEST
                EventParams testEventParams = new EventParams()
                {
                    City = "Ghent",
                };
                //List<Event> result = await EventService.GetEventsAsync(testEventParams);

                // Generate a dialog context for our dialog set.
                DialogContext dc = await findEventDialog._dialogSet.CreateContextAsync(turnContext, cancellationToken);

                if (date != null)
                {
                    var text = instance["datetime"][0]["text"].ToString();
                    try
                    {
                        DateTime d = ParseLuisTest.ParseDateEntitie(text);
                        eventParams.Date = d.ToString();
                    }
                    catch (Exception)
                    {

                    }
                }
                if (location != null)
                {
                    string loc = location[0].ToString();
                    eventParams.City = loc;
                }

                // Als er geen dialoog bezig is
                if (dc.ActiveDialog is null)
                {
                    switch (intent)
                    {
                        case "FindEventIntent":
                            // start new dialog
                            await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "GreetingIntent":
                            var reply = turnContext.Activity.CreateReply("Hi there! I can help you find events.");
                            List<CardAction> actions = new List<CardAction>()
                            {
                                new CardAction() { Title = "Find me an event!", Type = ActionTypes.ImBack, Value = "Find me an event!" }
                            };
                            reply.SuggestedActions = new SuggestedActions() { Actions = actions };
                            await turnContext.SendActivityAsync(reply);
                            break;
                        case "None":
                            //await turnContext.SendActivityAsync("I Do not understand what you are trying to say...");
                            break;
                        case "ThankIntent":
                            await turnContext.SendActivityAsync("No Problem!");
                            break;
                    }
                }
                else
                {
                    // Continue the dialog.
                    DialogTurnResult dialogTurnResult = await dc.ContinueDialogAsync(cancellationToken);

                    // If the dialog completed this turn, record the reservation info.
                    if (dialogTurnResult.Status is DialogTurnStatus.Complete)
                    {
                        // opgegeven waarden wegschrijven naar eventParam
                        eventParams = (EventParams)dialogTurnResult.Result;

                        // Send a confirmation message to the user (iets doen met de data)
                        await turnContext.SendActivityAsync(
                            $"I am looking for events with genre {eventParams.Genre} not further than {eventParams.Radius}km from {eventParams.City} on {eventParams.Date.ToString()}",
                            cancellationToken: cancellationToken);

                        // object terug leegmaken
                        eventParams = new EventParams();
                    }
                }

                // Save the updated dialog state into the conversation state.
                await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
        }
    }
}
