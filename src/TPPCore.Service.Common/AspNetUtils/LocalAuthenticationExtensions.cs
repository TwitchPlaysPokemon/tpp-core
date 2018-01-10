using Microsoft.AspNetCore.Builder;

namespace TPPCore.Service.Common.AspNetUtils
{
    public static class LocalAuthenticationExtensions
    {
        public static IApplicationBuilder UseLocalAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LocalAuthenticationMiddleware>();
        }
    }
}
