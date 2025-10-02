using System;
using System.Net.WebSockets;
using System.Text;
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

        /// <summary>
        /// Manages the full lifecycle of a WebSocket connection:
        /// - Receives messages
        /// - Closes socket on disconnect/error
        /// - Delegates message handling
        /// </summary>
        public async Task HandleConnectionAsync(string clientId, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[4 * 1024];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _manager.DisconnectClientAsync(clientId);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Delegate to existing message handler
                    await HandleMessageAsync(clientId, message, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection handling canceled for {ClientId}", clientId);
                await _manager.DisconnectClientAsync(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket connection for {ClientId}", clientId);
                await _manager.DisconnectClientAsync(clientId);
            }
        }

        /// <summary>
        /// Handles incoming JSON messages: "broadcast" / "direct"
        /// </summary>
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
