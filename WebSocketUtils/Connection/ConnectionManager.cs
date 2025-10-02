using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebSocketUtils.Connection
{
    /// <summary>
    /// ConnectionManager is a lightweight in-memory registry of active WebSocket connections.
    /// 
    /// Responsibilities:
    /// - Track sockets by client ID
    /// - Send messages to individual clients
    /// - Broadcast messages to all connected clients
    /// - Manage connection lifecycle (add/remove)
    /// 
    /// Note: This manager only handles sockets on the current server instance.
    /// It does not handle cross-instance communication — that's where a broker comes in.
    /// </summary>
    public class ConnectionManager
    {
        // Thread-safe dictionary of clientId → WebSocket
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        private readonly ILogger<ConnectionManager> _logger;

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a WebSocket connection to the registry.
        /// </summary>
        public void AddSocket(string id, WebSocket socket)
        {
            _sockets.TryAdd(id, socket);
            _logger.LogInformation("Added socket {Id}", id);
        }

        /// <summary>
        /// Removes a WebSocket connection from the registry and closes it gracefully.
        /// </summary>
        public async Task RemoveSocketAsync(string id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
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
                    _logger.LogWarning(ex, "Error closing socket {Id}", id);
                }
                finally
                {
                    _logger.LogInformation("Removed socket {Id}", id);
                }
            }
        }

        /// <summary>
        /// Sends a message to a single client by ID.
        /// If the socket is closed or fails, it will be removed.
        /// Virtual so it can be overridden (e.g., in a brokered manager).
        /// </summary>
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
                        _logger.LogWarning(ex, "Failed to send message to {Id}, removing socket.", clientId);
                        await RemoveSocketAsync(clientId);
                    }
                }
                else
                {
                    // Socket is no longer usable
                    await RemoveSocketAsync(clientId);
                }
            }
        }

        /// <summary>
        /// Broadcasts a message to all connected clients with throttled concurrency.
        /// Prevents overwhelming the server when sending to thousands of sockets.
        /// </summary>
        public async Task BroadcastAsync(string message, CancellationToken cancellationToken = default, int maxConcurrency = 100)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            using var throttler = new SemaphoreSlim(maxConcurrency);

            // Async broadcasting
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
                            _logger.LogWarning(ex, "Failed to broadcast to {Id}, removing socket.", clientId);
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

            // Wait for all throttled tasks
            await Task.WhenAll(tasks);
        }


        /// <summary>
        /// Gets the WebSocket associated with a client ID, or null if not found.
        /// </summary>
        public WebSocket? GetSocket(string clientId)
        {
            _sockets.TryGetValue(clientId, out var socket);
            return socket;
        }

        /// <summary>
        /// Returns all active client IDs.
        /// Useful for monitoring or admin tools.
        /// </summary>
        public IEnumerable<string> GetAllIds() => _sockets.Keys;

        /// <summary>
        /// Internal method for retrieving a socket.
        /// Functionally similar to GetSocket but bypasses logging/validation.
        /// </summary>
        public WebSocket? GetSocketInternal(string clientId) =>
            _sockets.TryGetValue(clientId, out var socket) ? socket : null;
    }
}
