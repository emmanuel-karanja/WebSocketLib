using WebSocketUtils.Middleware;
using Microsoft.AspNetCore.Builder;

namespace WebSocketUtils.Extensions
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds logging and telemetry middleware to the application.
        /// </summary>
        public static WebApplication UseLoggingAndTelemetry(this WebApplication app)
        {
            // Custom request logging
            app.UseMiddleware<LoggingMiddleware>();

            // Custom telemetry (e.g., metrics, traces)
            app.UseMiddleware<TelemetryMiddleware>();

            return app;
        }
    }
}
