using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using WebSocketUtils.Connection;
using WebSocketUtils.Services;

namespace WebSocketUtils.Demo.Services
{
    public class NotificationWebService : IWebSocketService
    {
        private readonly BrokeredConnectionManager _manager;
        private readonly ILogger<NotificationWebService> _logger;

        public NotificationWebService(BrokeredConnectionManager manager, ILogger<NotificationWebService> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleMessageAsync(string clientId, string messageText, CancellationToken cancellationToken = default)
        {
            using (LogContext.PushProperty("ClientId", clientId))
            {
                _logger.LogInformation("Received message: {Message}", messageText);

                try
                {
                    var doc = JsonDocument.Parse(messageText);

                    if (doc.RootElement.TryGetProperty("type", out var typeProp))
                    {
                        var type = typeProp.GetString();
                        switch (type)
                        {
                            case "broadcast":
                                await HandleBroadcastAsync(clientId, doc, cancellationToken);
                                break;

                            case "direct":
                                await HandleDirectAsync(clientId, doc, cancellationToken);
                                break;

                            default:
                                _logger.LogWarning("Unknown message type from {ClientId}: {Type}", clientId, type);
                                await _manager.SendMessageAsync(clientId, $"[Error] Unknown type: {type}", cancellationToken);
                                break;
                        }
                    }
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Invalid message format from {ClientId}: {Message}", clientId, messageText);
                    await _manager.SendMessageAsync(clientId, $"[Echo] {messageText}", cancellationToken);
                }
            }
        }

        private async Task HandleBroadcastAsync(string clientId, JsonDocument doc, CancellationToken cancellationToken = default)
        {
            var msg = doc.RootElement.GetProperty("message").GetString();
            if (!string.IsNullOrWhiteSpace(msg))
            {
                _logger.LogInformation("Broadcasting message from {ClientId}: {Message}", clientId, msg);
                await _manager.BroadcastMessageAsync($"[Broadcast from {clientId}]: {msg}", cancellationToken);
            }
        }

        private async Task HandleDirectAsync(string clientId, JsonDocument doc, CancellationToken cancellationToken = default)
        {
            var target = doc.RootElement.GetProperty("target").GetString();
            var msg = doc.RootElement.GetProperty("message").GetString();

            if (!string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(msg))
            {
                _logger.LogInformation("Sending direct message from {ClientId} to {Target}: {Message}", clientId, target, msg);
                await _manager.SendMessageAsync(target, $"[Direct from {clientId}]: {msg}", cancellationToken);
            }
        }
    }
}
