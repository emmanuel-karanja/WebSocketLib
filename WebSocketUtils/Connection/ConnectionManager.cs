using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebSocketUtils.Connection
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ILogger<ConnectionManager> _logger;

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        public void AddSocket(string id, WebSocket socket)
        {
            _sockets.TryAdd(id, socket);
            _logger.LogInformation("Added socket {Id}", id);
        }

        public async Task RemoveSocketAsync(string id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by manager", CancellationToken.None);
                _logger.LogInformation("Removed socket {Id}", id);
            }
        }

        public virtual async Task SendMessageAsync(string clientId, string message, CancellationToken cancellationToken=default)
        {
            if (_sockets.TryGetValue(clientId, out var socket) && socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        public async Task BroadcastAsync(string message, CancellationToken cancellationToken=default)
        {
            foreach (var socket in _sockets.Values)
            {
                if (socket.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
                }
            }
        }


        public WebSocket? GetSocket(string clientId)
        {
            _sockets.TryGetValue(clientId, out var socket);
            return socket;
        }

        public IEnumerable<string> GetAllIds() => _sockets.Keys;

        public WebSocket? GetSocketInternal(string clientId) => 
            _sockets.TryGetValue(clientId, out var socket) ? socket : null;
    }
}
