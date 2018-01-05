using System;
using System.Net;
using Microsoft.AspNetCore.Routing;

namespace TPPCore.Service.Common
{
    public class RestfulServerContext
    {
        public IPAddress Host;
        public int Port;
        public int RealPort;
        internal Action<RouteBuilder> routeBuilderAction;

        public RestfulServerContext()
        {

        }

        public Uri GetUri()
        {
            return new Uri($"http://{Host}:{RealPort}");
        }
    }
}
