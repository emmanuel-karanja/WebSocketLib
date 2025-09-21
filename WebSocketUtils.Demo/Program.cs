using WebSocketUtils.Connection;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebSocketUtils.Services;
using WebSocketUtils.Demo.Services;
using WebSocketUtils.Extensions;
using WebSocketUtils.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Setup Serilog from configuration
// ----------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // read from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ----------------------------
// Register brokers and connection manager via extension method
// ----------------------------
builder.Services.AddWebSocketBrokers(options =>
{
    options.RedisHost = builder.Configuration["WebSocketDemo:Redis:Host"] ?? "localhost";
    options.RedisPort = int.Parse(builder.Configuration["WebSocketDemo:Redis:Port"] ?? "6379");
    options.KafkaBootstrapServers = builder.Configuration["WebSocketDemo:Kafka:BootstrapServers"] ?? "";
});

builder.Services.AddSingleton<BrokeredConnectionManager>();
builder.Services.AddSingleton<IWebSocketService, NotificationWebService>();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// ----------------------------
// Middleware pipeline
// ----------------------------
// CorrelationID added
// Correlation first
app.UseMiddleware<CorrelationMiddleware>();

// Then your logging & telemetry
app.UseLoggingAndTelemetry();

// Built-in Serilog request logging
app.UseSerilogRequestLogging();

// Request logging and telemetry (custom middlewares)
app.UseLoggingAndTelemetry();

// Built-in ASP.NET logging (optional, but helps debug)
app.UseSerilogRequestLogging();

// Enable WebSockets globally
app.UseWebSockets();

// Map controllers (WebSocketController handles /api/websocket/ws)
app.MapControllers();

app.Run();
