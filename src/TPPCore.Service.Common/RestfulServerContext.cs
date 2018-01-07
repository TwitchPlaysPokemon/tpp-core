using System;
using System.Net;
using Microsoft.AspNetCore.Routing;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// Encapsulates data related to running the RESTful web server.
    /// </summary>
    public class RestfulServerContext
    {
        /// <summary>
        /// IP address supplied by configuration file.
        /// </summary>
        public IPAddress Host;

        /// <summary>
        /// Port number supplied by configuration file.
        /// </summary>
        public int Port;

        /// <summary>
        /// Port number that the web host is bound to.
        /// </summary>
        public int RealPort;

        internal Action<RouteBuilder> routeBuilderAction;

        public RestfulServerContext()
        {

        }

        /// <summary>
        /// Returns a HTTP Uri prefilled with the host and port number.
        /// </summary>
        public Uri GetUri()
        {
            return new Uri($"http://{Host}:{RealPort}");
        }
    }
}
