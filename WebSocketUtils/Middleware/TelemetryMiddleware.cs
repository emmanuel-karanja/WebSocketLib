using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace WebSocketUtils.Middleware
{
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;

        public TelemetryMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            await _next(context);
            sw.Stop();
            Console.WriteLine($"Request {context.Request.Path} took {sw.ElapsedMilliseconds}ms");
        }
    }
}
