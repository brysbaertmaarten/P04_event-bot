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
using Microsoft.Bot.Builder.Location;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EventBot.Models.FacebookChannelData;

namespace EventBot
{
    public class EventBot : IBot
    {
        public static readonly string LuisKey = "EventBot";

        private readonly EventBotAccessors accessors;
        private FindEventDialog FindEventDialog { get; }
        private readonly BotServices services;
        private readonly EventService eventService;
        public static int pageCount = 0;

        DialogTurnResult dialogTurnResult = new DialogTurnResult(DialogTurnStatus.Empty);

        public EventBot(EventBotAccessors accessors, BotServices services, EventService eventService)
        {
            this.accessors = accessors;
            this.services = services ?? throw new System.ArgumentNullException(nameof(services));
            this.eventService = eventService;
            if (!services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration....");
            }
            FindEventDialog = new FindEventDialog(accessors, eventService, services);
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                RecognizerResult recognizerResult = new RecognizerResult();
                string intent = "";
                string message = "";
                if (turnContext.Activity.Text != null)
                {
                    // input doorsturen naar LUIS API
                    message = turnContext.Activity.Text.ToLower();
                    recognizerResult = await services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                    intent = recognizerResult?.GetTopScoringIntent().intent;
                }

                // Generate a dialog context for our dialog set.
                DialogContext dc = await FindEventDialog.CreateContextAsync(turnContext); 

                // eventParams ophalen uit store
                EventParams eventParams = await accessors.EventParamState.GetAsync(turnContext, () => null, cancellationToken);

                // Als er geen dialoog bezig is
                if (dc.ActiveDialog is null)
                {
                    var reply = turnContext.Activity.CreateReply();
                    string suggestedAnswer;
                    switch (message)
                    {
                        case "change location":
                            eventParams.City = null; eventParams.GeoHash = null; pageCount = 0; eventParams.Radius = 0;
                            await accessors.EventParamState.SetAsync(turnContext, eventParams);
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "change genre":
                            eventParams.Genre = null; pageCount = 0;
                            await accessors.EventParamState.SetAsync(turnContext, eventParams);
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "more":
                            pageCount += 1;
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        case "change date":
                            eventParams.StartDate = null; pageCount = 0;
                            await accessors.EventParamState.SetAsync(turnContext, eventParams);
                            dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                            break;
                        default:
                            switch (intent)
                            {
                                case "FindEventIntent":
                                    // Check if there are entities recognized (moet voor het begin van het dialog)
                                    eventParams = ParseLuis.GetEntities(recognizerResult);
                                    await accessors.EventParamState.SetAsync(turnContext, eventParams);
                                    pageCount = 0;
                                    // start new dialog 
                                    dialogTurnResult = await dc.BeginDialogAsync("findEventDialog", null, cancellationToken);
                                    break;
                                case "GreetingIntent":
                                    reply = turnContext.Activity.CreateReply();
                                    reply.Text = MessageService.GetMessage("GreetingAnswer"); // geeft een random antwoord van de greetingAnswer list terug
                                    suggestedAnswer = MessageService.GetMessage("FindEventSuggestedAnswer");
                                    reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { suggestedAnswer });
                                    await turnContext.SendActivityAsync(reply);
                                    break;
                                case "ThankIntent":
                                    string thankReply = MessageService.GetMessage("ThankAnswer");
                                    await turnContext.SendActivityAsync(thankReply);
                                    break;
                                case "GoodByeIntent":
                                    string goodByeReply = MessageService.GetMessage("GoodByeAnswer");
                                    await turnContext.SendActivityAsync(goodByeReply);
                                    break;
                                case "GetAgeIntent":
                                    reply.Text = MessageService.GetMessage("GetAgeAnswer");
                                    await turnContext.SendActivityAsync(reply);
                                    break;
                                case "HowAreYouIntent":
                                    reply.Text = MessageService.GetMessage("HowAreYouAnswer");
                                    suggestedAnswer = MessageService.GetMessage("FindEventSuggestedAnswer");
                                    reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { suggestedAnswer });
                                    await turnContext.SendActivityAsync(reply);
                                    break;
                                case "GetNameIntent":
                                    reply.Text = MessageService.GetMessage("GetNameAnswer");
                                    suggestedAnswer = MessageService.GetMessage("FindEventSuggestedAnswer");
                                    reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { suggestedAnswer });
                                    await turnContext.SendActivityAsync(reply);
                                    break;
                                case "TellJokeIntent":
                                    reply.Text = MessageService.GetMessage("TellJokeAnswer");
                                    await turnContext.SendActivityAsync(reply);
                                    reply.Text = await JokeService.GetJoke();
                                    //reply.Text = "How does a computer get drunk? It takes screenshots.";
                                    await turnContext.SendActivityAsync(reply);
                                    break;
                                case "None":
                                    var noneReply = turnContext.Activity.CreateReply();
                                    noneReply.Text = MessageService.GetMessage("NoneAnswer");
                                    suggestedAnswer = MessageService.GetMessage("FindEventSuggestedAnswer");
                                    noneReply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { suggestedAnswer });
                                    await turnContext.SendActivityAsync(noneReply);
                                    break;
                                case "EmptyIntent":
                                    break;
                            }
                            break;
                    }
                }
                else
                {
                    // Continue the dialog.
                    if (dc.ActiveDialog.Id == "locationPrompt")
                    {
                        if (turnContext.Activity.Attachments != null)
                        {
                            turnContext.Activity.Text = "nearby";
                        }
                    }
                    switch (message)
                    {
                        case "end":
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
                    // bij het wachten toont de bot een "typing" teken
                    var typing = turnContext.Activity.CreateReply();
                    typing.Type = ActivityTypes.Typing;
                    await turnContext.SendActivityAsync(typing);

                    List<Event> events = await eventService.GetEventsAsync(eventParams, pageCount);
                    var reply = turnContext.Activity.CreateReply();

                    if (events.Count() != 0)
                    {
                        reply.Text = CreateActivity.GetTextForFoundEvents(eventParams);
                        reply.Attachments = CreateActivity.GetAttachementForFoundEvents(events);
                        reply.AttachmentLayout = "carousel";
                        List<string> suggestedActions = new List<string>();
                        if (events.Count() == 10)
                        {
                            suggestedActions.Add("more");
                        }
                        suggestedActions.Add("change genre"); suggestedActions.Add("change location"); suggestedActions.Add("change date");
                        reply.SuggestedActions = CreateActivity.CreateSuggestedAction(suggestedActions);
                    }
                    else
                    {
                        reply.Text = CreateActivity.GetTextForNoEventsFound(eventParams);
                        if (eventParams.Genre == null || eventParams.Genre.ToLower() == "none")
                        {
                            reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { "change location", "change date" });
                        }
                        else
                        {
                            reply.SuggestedActions = CreateActivity.CreateSuggestedAction(new List<string>() { "change genre", "change location", "change date" });
                        }
                    }
               
                    // send reply
                    await turnContext.SendActivityAsync(reply);
                }
            }

            // als de gebruiker via fb chat, sla dan een aantal gegevens op
            if (turnContext.Activity.ChannelId == Microsoft.Bot.Connector.Channels.Facebook)
            {
                dynamic jsonObject = JsonConvert.DeserializeObject(turnContext.Activity.ChannelData.ToString());
                string userId = jsonObject["sender"]["id"];
                // userProfile ophalen van db
                UserProfile userProfile = await accessors.UserProfileState.GetAsync(turnContext, () => null, cancellationToken);
                if (userProfile == null)
                {
                    userProfile = new UserProfile();
                }
                userProfile.Id = userId;
                userProfile.ChannelId = turnContext.Activity.ChannelId;
                await accessors.UserProfileState.SetAsync(turnContext, userProfile);

                //var getStartedReply = turnContext.Activity.CreateReply();
                //getStartedReply.ChannelData = JObject.FromObject(new { get_started = new { payload = "test" } });
                //await turnContext.SendActivityAsync(getStartedReply);
            }

            // Save the updated dialogState into the conversation state.
            await accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            // Save the updated userProfileState into the user state. (Azure CosmoDB)
            await accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    }
}
