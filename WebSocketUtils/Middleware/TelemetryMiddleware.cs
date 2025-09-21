using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog.Context;

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

            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                _logger.LogInformation("Incoming HTTP request");

                await _next(context); // pass down the pipeline

                sw.Stop();
                _logger.LogInformation("Completed HTTP request in {Elapsed}ms", sw.ElapsedMilliseconds);
            }
        }
    }
}
