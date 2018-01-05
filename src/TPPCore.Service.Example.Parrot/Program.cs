using System;
using TPPCore.Service.Common;

namespace TPPCore.Service.Example.Parrot
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new ParrotService();
            ServiceRunner.Run(service, args);
        }
    }
}
