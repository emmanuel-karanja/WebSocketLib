using Microsoft.AspNetCore.Http;
using Serilog;

namespace WebSocketUtils.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            Log.Information("Handling request: {Path}", context.Request.Path);
            await _next(context);
            Log.Information("Finished handling request.");
        }
    }
}
