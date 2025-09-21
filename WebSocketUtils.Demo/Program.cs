using WebSocketUtils.Demo.Extensions;
using WebSocketUtils.Connection;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using WebSocketUtils.Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Setup Serilog from configuration
// ----------------------------
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .WriteTo.Console()
);

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
builder.Services.AddSingleton<IWebSocketMessageService, WebSocketMessageService>();


// Add controllers
builder.Services.AddControllers();

// ----------------------------
// Build app
// ----------------------------
var app = builder.Build();

// ----------------------------
// Middleware pipeline
// ----------------------------

// Logging & telemetry
app.UseLoggingAndTelemetry();

// Enable WebSockets globally
app.UseWebSockets();

// Map controllers (WebSocketController handles /ws)
app.MapControllers();

// ----------------------------
// Run the application
// ----------------------------
app.Run();
