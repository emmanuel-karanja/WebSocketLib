using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebSocketUtils.Connection
{
    /// <summary>
    /// ConnectionManager is a lightweight in-memory registry of active WebSocket connections.
    /// Tracks sockets by client ID and also maintains per-IP mappings.
    /// </summary>
    public class ConnectionManager
    {
        // Thread-safe dictionary of clientId → WebSocket
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        // Tracks IP → HashSet of clientIds
        private readonly ConcurrentDictionary<string, HashSet<string>> _ipToClients = new();

        private readonly ILogger<ConnectionManager> _logger;

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a WebSocket connection to the registry with associated IP.
        /// </summary>
        public void AddSocket(string clientId, WebSocket socket, string ip)
        {
            _sockets.TryAdd(clientId, socket);

            var clients = _ipToClients.GetOrAdd(ip, _ => new HashSet<string>());
            lock (clients) // lock needed because HashSet is not thread-safe
            {
                clients.Add(clientId);
            }

            _logger.LogInformation("Added socket {ClientId} for IP {IP}", clientId, ip);
        }

        /// <summary>
        /// Removes a WebSocket connection from the registry and closes it gracefully.
        /// Also updates the IP → client map.
        /// </summary>
        public async Task RemoveSocketAsync(string clientId)
        {
            if (_sockets.TryRemove(clientId, out var socket))
            {
                // Remove from IP mapping
                foreach (var kvp in _ipToClients)
                {
                    var ip = kvp.Key;
                    var clients = kvp.Value;

                    lock (clients)
                    {
                        if (clients.Remove(clientId))
                        {
                            if (clients.Count == 0)
                                _ipToClients.TryRemove(ip, out _);
                            break;
                        }
                    }
                }

                try
                {
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closed by manager",
                            CancellationToken.None
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing socket {ClientId}", clientId);
                }
                finally
                {
                    _logger.LogInformation("Removed socket {ClientId}", clientId);
                }
            }
        }

        /// <summary>
        /// Get all clientIds for a given IP
        /// </summary>
        public IReadOnlyCollection<string> GetConnectionsByIp(string ip)
        {
            if (_ipToClients.TryGetValue(ip, out var clients))
            {
                lock (clients)
                {
                    return clients.ToList().AsReadOnly();
                }
            }
            return Array.Empty<string>();
        }

        // --------------------- Existing methods unchanged --------------------- //

        public virtual async Task SendMessageAsync(string clientId, string message, CancellationToken cancellationToken = default)
        {
            if (_sockets.TryGetValue(clientId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        var buffer = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send message to {ClientId}, removing socket.", clientId);
                        await RemoveSocketAsync(clientId);
                    }
                }
                else
                {
                    await RemoveSocketAsync(clientId);
                }
            }
        }

        public async Task BroadcastAsync(string message, CancellationToken cancellationToken = default, int maxConcurrency = 100)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            using var throttler = new SemaphoreSlim(maxConcurrency);

            var tasks = _sockets.Select(async entry =>
            {
                await throttler.WaitAsync(cancellationToken);
                try
                {
                    var (clientId, socket) = (entry.Key, entry.Value);

                    if (socket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast to {ClientId}, removing socket.", clientId);
                            await RemoveSocketAsync(clientId);
                        }
                    }
                    else
                    {
                        await RemoveSocketAsync(clientId);
                    }
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
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
