using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace api.Sidecar
{
    public static class SidecarEndpoints
    {
        public static IEndpointRouteBuilder MapSidecarEndpoints(this IEndpointRouteBuilder app)
        {
            // Unauthenticated liveness probe for Electron.
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            // Graceful stop. On Windows, killing a child process is a hard terminate,
            // so Electron should call this first and only kill as a fallback.
            app.MapPost("/shutdown", (IHostApplicationLifetime lifetime) =>
            {
                lifetime.StopApplication();
                return Results.Accepted();
            });

            return app;
        }
    }
}
