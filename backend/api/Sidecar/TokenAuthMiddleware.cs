using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace api.Sidecar
{
    // Any local process (or a web page in the user's browser) can reach 127.0.0.1,
    // so every request must carry the per-launch token Electron handed to us.
    public class TokenAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly byte[]? _token;

        public TokenAuthMiddleware(RequestDelegate next, SidecarOptions options)
        {
            _next = next;
            _token = string.IsNullOrEmpty(options.Token) ? null : Encoding.UTF8.GetBytes(options.Token);
        }

        public Task InvokeAsync(HttpContext context)
        {
            // No token configured = local development (enforced at startup).
            if (_token == null || context.Request.Path.StartsWithSegments("/health"))
                return _next(context);

            var provided = Encoding.UTF8.GetBytes(context.Request.Headers[SidecarOptions.TokenHeader].ToString());
            if (!CryptographicOperations.FixedTimeEquals(provided, _token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
