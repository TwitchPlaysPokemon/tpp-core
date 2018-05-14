using System;
using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Represents user initiated events such as subscription, hosting, etc.
    /// </summary>
    public class LoyaltyEvent : ChatMessage
    {
        public LoyaltyEvent() : base() {
            Topic = ChatTopics.Loyalty;
        }
    }
}
