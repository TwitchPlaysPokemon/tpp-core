using System;
using TPPCore.Service.Common;

namespace TPPCore.Service.Chat
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new ChatService();
            ServiceRunner.Run(service, args);
        }
    }
}
