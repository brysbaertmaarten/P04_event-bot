using EventBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot
{
    public class EventBotAccessors
    {
        public EventBotAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public static string DialogStateAccessorKey { get; } = "EventBotAccessors.DialogState";
        public static string EventParamStateAccessorKey { get; } = "EventBotAccessors.EventParamState";
        public static string DidWelcomeStateAccessorKey { get; } = "EventBotAccessors.DidWelcomeState";

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }
        public IStatePropertyAccessor<EventParams> EventParamState { get; set; }
        public IStatePropertyAccessor<bool> DidWelcomeState { get; set; }

        public ConversationState ConversationState { get; }
    }
}
