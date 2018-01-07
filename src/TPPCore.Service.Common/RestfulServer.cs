using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Routing;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// Configures and encapsulates a ASP.Net Kestrel web server.
    /// </summary>
    public class RestfulServer
    {
        public readonly RestfulServerContext Context;
        public IWebHost AspNetWebHost { get; private set; }

        public RestfulServer(IPAddress host, int port = 0)
        {
            Context = new RestfulServerContext();
            Context.Host = host;
            Context.Port = port;
        }

        /// <summary>
        /// Build the ASP.Net web host.
        /// </summary>
        public void BuildWebHost()
        {
            configureHost();
        }

        void configureHost()
        {
            AspNetWebHost = WebHost.CreateDefaultBuilder()
                .ConfigureServices(services => {
                    services.AddSingleton<RestfulServerContext>(Context);
                })
                .UseStartup<StartupRestful>()
                .UseKestrel(options =>
                {
                    options.Listen(Context.Host, Context.Port);
                })
                .Build();
        }

        /// <summary>
        /// Registers endpoint handlers.
        /// </summary>
        /// <param name="routeBuilder">
        /// A delegate that will build up the routes using
        /// <see cref="RouteBuilder"/>.
        /// </param>
        public void UseRoute(Action<RouteBuilder> routeBuilder)
        {
            Context.routeBuilderAction = routeBuilder;
        }

        /// <summary>
        /// Sets the bound port number to the context.
        /// </summary>
        /// <remarks>
        /// The method is required after starting the web host as it may
        /// be configured to use a free port rather than a fixed port
        /// </remarks>
        public void UpdateRealPort()
        {
            var serverAddressesFeature = AspNetWebHost.ServerFeatures.Get<IServerAddressesFeature>();

            Context.RealPort = getPort(serverAddressesFeature.Addresses);
        }

        static int getPort(ICollection<string> addresses)
        {
            foreach (var address in addresses.ToImmutableList())
            {
                var uri = new Uri(address);
                return uri.Port;
            }

            throw new ArgumentException("No addresses");
        }
    }
}
