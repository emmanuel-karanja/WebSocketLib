// LoggingMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WebSocketUtils.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("Request starting: {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context); // 👈 MUST call this

            _logger.LogInformation("Request finished: {StatusCode}", context.Response.StatusCode);
        }
    }
}
