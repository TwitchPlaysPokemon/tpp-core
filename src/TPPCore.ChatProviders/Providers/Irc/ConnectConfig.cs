namespace TPPCore.ChatProviders.Providers.Irc
{
    public class ConnectConfig
    {
        public string Host;
        public int Port;
        public bool Ssl;
        public int Timeout;
        public string Nickname;
        public string Password;
        public string[] Channels;
        public int MaxMessageBurst;
        public int CounterPeriod;
    }
}
