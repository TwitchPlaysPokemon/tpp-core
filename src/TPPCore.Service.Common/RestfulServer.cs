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

        public void UseRoute(Action<RouteBuilder> routeBuilder)
        {
            Context.routeBuilderAction = routeBuilder;
        }

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
