// CorrelationMiddleware.cs
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Threading.Tasks;

namespace WebSocketUtils.Middleware
{
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            context.Items["CorrelationId"] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                context.Response.Headers.Append("X-Correlation-Id", correlationId);
                await _next(context);
            }
        }
    }
}
