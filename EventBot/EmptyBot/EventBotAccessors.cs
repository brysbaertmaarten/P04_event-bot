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
        public static string EventAccessorKey { get; } = "EventBotAccessors.Event";

        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }
        public IStatePropertyAccessor<EventParams> EventAccessor { get; set; }

        public ConversationState ConversationState { get; }
    }
}
