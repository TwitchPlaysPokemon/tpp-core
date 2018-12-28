namespace TPPCore.Service.Example.Parrot
{
    public class ParrotConfig
    {
        public DatabaseConfig database;

        public Parrot parrot;

        public class DatabaseConfig
        {
            /// <summary>
            /// The database to connect to.
            /// </summary>
            public string database;
            /// <summary>
            /// Where the database is hosted.
            /// </summary>
            public string host;
            /// <summary>
            /// The application name to use.
            /// </summary>
            public string appname;
            /// <summary>
            /// The username to log into.
            /// </summary>
            public string username;
            /// <summary>
            /// The password used to log in.
            /// </summary>
            public string password;
            /// <summary>
            /// The port to connect to.
            /// </summary>
            public int port;
            /// <summary>
            /// The location of the setup file.
            /// </summary>
            public string setup;
        }

        public class Parrot
        {
            /// <summary>
            /// The interval to send every broadcast.
            /// </summary>
            public int broadcastInterval;
            /// <summary>
            /// The interval to send recent broadcasts.
            /// </summary>
            public int recentIntervalCount;
        }
    }
}
