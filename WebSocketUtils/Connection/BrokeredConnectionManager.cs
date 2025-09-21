using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using WebSocketUtils.Brokers;

namespace WebSocketUtils.Connection
{
    public class BrokeredConnectionManager : ConnectionManager
    {
        private readonly IMessageBroker _broker;
        private readonly ILogger<BrokeredConnectionManager> _logger;

        public BrokeredConnectionManager(IMessageBroker broker, ILogger<BrokeredConnectionManager> logger)
            : base(logger)
        {
            _broker = broker;
            _logger = logger;
        }

        public async Task HandleConnectionAsync(string clientId, WebSocket socket, CancellationToken cancellationToken=default)
        {
            AddSocket(clientId, socket);

            await _broker.SubscribeAsync("broadcast", async message =>
            {
                await SendMessageAsync(clientId, message, cancellationToken);
            });

            var buffer = new byte[1024 * 4];

            try
            {
                while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await RemoveSocketAsync(clientId);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Received from {ClientId}: {Message}", clientId, message);

                    await _broker.PublishAsync("broadcast", message);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection handling canceled for {ClientId}", clientId);
                await RemoveSocketAsync(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket connection for {ClientId}", clientId);
                await RemoveSocketAsync(clientId);
            }
        }

        public override async Task SendMessageAsync(string clientId, string message, CancellationToken cancellationToken=default)
        {
            var socket = GetSocket(clientId);
            if (socket != null && socket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        public WebSocket? GetSocket(string clientId)
        {
            return GetAllIds()
                   .Where(id => id == clientId)
                   .Select(id => GetSocketInternal(id))
                   .FirstOrDefault();
        }

        public async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken)
        {
            foreach (var id in GetAllIds())
            {
                await SendMessageAsync(id, message, cancellationToken);
            }
        }

        public IDictionary<string, WebSocket> GetAllSockets()
        {
            var result = new Dictionary<string, WebSocket>();
            foreach (var id in GetAllIds())
            {
                var socket = GetSocket(id);
                if (socket != null)
                {
                    result[id] = socket;
                }
            }
            return result;
        }
    }
}
