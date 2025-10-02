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
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext() // Uses LogContext for correlation
    .Enrich.WithProperty("Application", "WebSocketDemo") // optional static property
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
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

// Register ConnectionManager and BrokeredConnectionManager in DI
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddSingleton<BrokeredConnectionManager>();

// Register WebSocket services
builder.Services.AddSingleton<IWebSocketService, NotificationWebService>();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// ----------------------------
// Middleware pipeline
// ----------------------------
// CorrelationID middleware first
app.UseMiddleware<CorrelationMiddleware>();

// Custom logging and telemetry middleware
app.UseLoggingAndTelemetry();

// Built-in Serilog request logging
app.UseSerilogRequestLogging();

// Enable WebSockets globally
app.UseWebSockets();

// Map controllers (WebSocketController handles /api/websocket/ws)
app.MapControllers();

app.Run();
