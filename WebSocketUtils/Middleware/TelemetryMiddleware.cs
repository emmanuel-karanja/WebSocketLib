using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WebSocketUtils.Middleware
{
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TelemetryMiddleware> _logger;

        public TelemetryMiddleware(RequestDelegate next, ILogger<TelemetryMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            // Include request path and method in the LogContext
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                await _next(context); // propagate

                sw.Stop();
                _logger.LogInformation("Request completed in {ElapsedMilliseconds}ms",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}
