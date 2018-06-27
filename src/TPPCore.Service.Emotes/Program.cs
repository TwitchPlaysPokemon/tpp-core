using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new EmoteService();
            ServiceRunner.Run(service, args);
        }
    }
}
