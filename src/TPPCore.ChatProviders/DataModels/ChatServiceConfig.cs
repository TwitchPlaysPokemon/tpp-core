using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    public class ChatServiceConfig
    {
        public ChatConfig chat;
        public class ChatConfig
        {
            /// <summary>
            /// A list of chat clients.
            /// </summary>
            public List<ClientConfig> clients;

            public class ClientConfig
            {
                /// <summary>
                /// Friendly name of the provider.
                /// </summary>
                public string client;
                /// <summary>
                /// Website or server endpoint name.
                /// </summary>
                public string provider;
                /// <summary>
                /// URL of the provider.
                /// </summary>
                public string host;
                /// <summary>
                /// Port to connect to.
                /// </summary>
                public int port;
                /// <summary>
                /// Whether to use ssl.
                /// </summary>
                public bool ssl;
                /// <summary>
                /// Socket timeout in milliseconds
                /// </summary>
                public int timeout;
                /// <summary>
                /// Nickname to use.
                /// </summary>
                public string nickname;
                /// <summary>
                /// Password to connect to chat.
                /// </summary>
                public string password;
                /// <summary>
                /// Ouath token (unique to mixer).
                /// </summary>
                public string oauthToken;
                /// <summary>
                /// Client ID is twitch unique, used for the api.
                /// </summary>
                public string client_id;
                /// <summary>
                /// Channels to connect to.
                /// </summary>
                public string[] channels;
                /// <summary>
                /// Rate limiting configuration.
                /// </summary>
                public RateLimitConfig rateLimit;

                public class RateLimitConfig
                {
                    /// <summary>
                    /// Amount of messages that can be sent before being rate limited.
                    /// </summary>
                    public int maxMessageBurst;
                    /// <summary>
                    /// Time in milliseconds.
                    /// </summary>
                    public int counterPeriod;
                }
            }
        }
    }
}
