using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using TPPCore.Utils;

namespace TPPCore.Service.Common.AspNetUtils
{
    /// <summary>
    /// Provides authentication using a locally configured password.
    /// </summary>
    class LocalAuthenticationMiddleware
    {
        public const string PasswordHeaderKey = "X-Local-Authentication-Password";
        private readonly RequestDelegate _next;
        private readonly RestfulServerContext restfulContext;
        private ConstantTimeComparer comparer;

        public LocalAuthenticationMiddleware(RequestDelegate next,
        RestfulServerContext restfulContext)
        {
            _next = next;
            this.restfulContext = restfulContext;

            loadPassword();
        }

        private void loadPassword()
        {
            var password = restfulContext.LocalAuthenticationPassword;

            Debug.Assert(password != null);
            Debug.Assert(password.Length > 0);

            comparer = new ConstantTimeComparer(password);
        }

        public async Task Invoke(HttpContext context)
        {
            var passwordHeader = context.Request.Headers[PasswordHeaderKey];
            var targetPassword = "";

            if (passwordHeader.Count > 0)
            {
                targetPassword = passwordHeader[0];
            }

            if (!comparer.CheckEquality(targetPassword))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            await this._next(context);
        }
    }
}
