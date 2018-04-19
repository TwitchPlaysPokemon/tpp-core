using TPPCore.Service.Common;

namespace TPPCore.Service.ChatLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new ChatLoggerService();
            ServiceRunner.Run(service, args);
        }
    }
}
