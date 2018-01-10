using System;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TPPCore.Service.Common.AspNetUtils;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// ASP.Net start up file.
    /// </summary>
    public class StartupRestful
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        RestfulServerContext restfulContext;

        public StartupRestful(RestfulServerContext restfulContext)
        {
            this.restfulContext = restfulContext;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Order of the middleware is important
            configureAuthentication(app);
            configureRoutes(app);
        }

        private void configureRoutes(IApplicationBuilder app)
        {
            var routeBuilder = new RouteBuilder(app);

            if (restfulContext.routeBuilderAction != null) {
                logger.Debug("We got routes. Adding them.");
                restfulContext.routeBuilderAction(routeBuilder);
            }

            app.UseRouter(routeBuilder.Build());
        }

        private void configureAuthentication(IApplicationBuilder app)
        {
            if (restfulContext.LocalAuthenticationPassword != null)
            {
                app.UseLocalAuthentication();
                logger.Debug("Service using authentication.");
            }
            else
            {
                logger.Warn("Authentication not configured.");
            }
        }
    }
}
