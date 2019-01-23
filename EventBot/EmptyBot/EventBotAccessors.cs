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
        public EventBotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        // conversationState
        public static string DialogStateAccessorKey { get; } = "EventBotAccessors.DialogState";
        public static string EventParamStateAccessorKey { get; } = "EventBotAccessors.EventParamState";
        public IStatePropertyAccessor<DialogState> DialogState { get; set; }
        public IStatePropertyAccessor<EventParams> EventParamState { get; set; }

        // userState
        public static string UserProfileName { get; } = "UserProfile";
        public IStatePropertyAccessor<UserProfile> UserProfileState { get; set; }
        
        public ConversationState ConversationState { get; }
        public UserState UserState { get; }
    }
}
