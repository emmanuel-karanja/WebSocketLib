using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebSocketUtils.Connection;

namespace WebSocketUtils.Demo.Services
{
    public class WebSocketMessageService : IWebSocketMessageService
    {
        private readonly BrokeredConnectionManager _manager;
        private readonly ILogger<WebSocketMessageService> _logger;

        public WebSocketMessageService(BrokeredConnectionManager manager, ILogger<WebSocketMessageService> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleMessageAsync(string clientId, string messageText, CancellationToken cancellationToken=default)
        {
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
                // Echo raw message back
                await _manager.SendMessageAsync(clientId, $"[Echo] {messageText}", cancellationToken);
            }
        }

        private async Task HandleBroadcastAsync(string clientId, JsonDocument doc, CancellationToken cancellationToken=default)
        {
            var msg = doc.RootElement.GetProperty("message").GetString();
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await _manager.BroadcastMessageAsync($"[Broadcast from {clientId}]: {msg}", cancellationToken);
            }
        }

        private async Task HandleDirectAsync(string clientId, JsonDocument doc, CancellationToken cancellationToken=default)
        {
            var target = doc.RootElement.GetProperty("target").GetString();
            var msg = doc.RootElement.GetProperty("message").GetString();
            if (!string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(msg))
            {
                await _manager.SendMessageAsync(target, $"[Direct from {clientId}]: {msg}", cancellationToken);
            }
        }
    }
}
