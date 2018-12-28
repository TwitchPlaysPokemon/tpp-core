namespace TPPCore.Service.Common
{
    internal class ServiceRunnerConfig
    {
#pragma warning disable 649
        public ServiceConfig service;

        public class ServiceConfig
        {
            /// <summary>
            /// The type of pubsub client to use.
            /// </summary>
            public string pubSub;
        }

        public RedisConfig redis;

        public class RedisConfig
        {
            /// <summary>
            /// The host for redis.
            /// </summary>
            public string host;
            /// <summary>
            /// The port for redis.
            /// </summary>
            public int port;
            /// <summary>
            /// Any extra parameters.
            /// </summary>
            public string extra;
            /// <summary>
            /// The database number.
            /// </summary>
            public int db;
        }

        public RestfulConfig restful;

        public class RestfulConfig
        {
            /// <summary>
            /// The restful host.
            /// </summary>
            public string host;
            /// <summary>
            /// The port to start the restful server on.
            /// </summary>
            public int port;
            /// <summary>
            /// Password to authenticate services.
            /// </summary>
            public string localAuthenticationPassword;
        }
#pragma warning restore 649
    }
}
