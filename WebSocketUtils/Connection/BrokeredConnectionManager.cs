using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using WebSocketUtils.Brokers;

namespace WebSocketUtils.Connection
{
    /// <summary>
    /// BrokeredConnectionManager integrates WebSocket connections (via ConnectionManager)
    /// with a distributed message broker (via IMessageBroker).
    /// 
    /// - Each client can SUBSCRIBE to one or more topics.
    /// - Messages are PUBLISHED to topics and delivered to all clients subscribed to that topic.
    /// - Ensures only one subscription per topic per server instance (avoids duplicates).
    /// 
    /// This pattern allows scaling WebSocket servers horizontally while keeping
    /// topic-based pub-sub semantics consistent across instances.
    /// </summary>
    public class BrokeredConnectionManager
    {
        private readonly IMessageBroker _broker;               // External broker (Redis, Kafka, NATS, etc.)
        private readonly ConnectionManager _connectionManager; // Local socket registry
        private readonly ILogger<BrokeredConnectionManager> _logger;

        // Tracks which topics each clientId is subscribed to
        private readonly Dictionary<string, HashSet<string>> _clientTopics = new();

        // Tracks topics already subscribed at the server instance level
        // → prevents duplicate broker subscriptions
        private readonly HashSet<string> _subscribedTopics = new();

        public BrokeredConnectionManager(
            IMessageBroker broker,
            ConnectionManager connectionManager,
            ILogger<BrokeredConnectionManager> logger)
        {
            _broker = broker;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        /// <summary>
        /// Handles a single WebSocket connection lifecycle:
        /// - Registers socket with ConnectionManager
        /// - Listens for incoming client messages
        /// - Supports simple text commands:
        ///   - "SUB:topic" → subscribe client to a topic
        ///   - "PUB:topic:payload" → publish payload to a topic
        /// - Cleans up on socket close/error
        /// </summary>
        public async Task HandleConnectionAsync(
            string clientId,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            // Register socket locally
            _connectionManager.AddSocket(clientId, socket);
            _clientTopics[clientId] = new HashSet<string>();

            var buffer = new byte[1024 * 4];

            try
            {
                // Main receive loop
                while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    // If client closed connection
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await RemoveClientAsync(clientId);
                        break;
                    }

                    // Decode message
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Received from {ClientId}: {Message}", clientId, message);

                    // Simple text protocol: SUB / PUB
                    if (message.StartsWith("SUB:"))
                    {
                        var topic = message.Substring(4).Trim();
                        await SubscribeClientToTopicAsync(clientId, topic);
                    }
                    else if (message.StartsWith("PUB:"))
                    {
                        // Format: PUB:topic:payload
                        var parts = message.Split(':', 3);
                        if (parts.Length == 3)
                        {
                            var topic = parts[1];
                            var payload = parts[2];
                            await _broker.PublishAsync(topic, payload);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection handling canceled for {ClientId}", clientId);
                await RemoveClientAsync(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket connection for {ClientId}", clientId);
                await RemoveClientAsync(clientId);
            }
        }

        /// <summary>
        /// Subscribes a client to a given topic.
        /// Ensures:
        /// - Only one broker subscription per topic per server instance
        /// - Local fan-out delivers messages to all clients subscribed to that topic
        /// </summary>
        private async Task SubscribeClientToTopicAsync(string clientId, string topic)
        {
            // Subscribe server instance to broker topic if not already
            if (!_subscribedTopics.Contains(topic))
            {
                _subscribedTopics.Add(topic);

                // One subscription per topic, local fan-out to clients
                await _broker.SubscribeAsync(topic, async message =>
                {
                    foreach (var kvp in _clientTopics)
                    {
                        var (cid, topics) = (kvp.Key, kvp.Value);
                        if (topics.Contains(topic))
                        {
                            await _connectionManager.SendMessageAsync(cid, message);
                        }
                    }
                });

                _logger.LogInformation("Subscribed server to topic {Topic}", topic);
            }

            // Track client subscription
            _clientTopics[clientId].Add(topic);
            _logger.LogInformation("Client {ClientId} subscribed to {Topic}", clientId, topic);
        }

        /// <summary>
        /// Removes client from local registries and closes socket.
        /// Called when a connection is closed or errors out.
        /// </summary>
        private async Task RemoveClientAsync(string clientId)
        {
            if (_clientTopics.ContainsKey(clientId))
                _clientTopics.Remove(clientId);

            await _connectionManager.RemoveSocketAsync(clientId);
        }

        /// <summary>
        /// Send a direct message to one client by ID.
        /// </summary>
        public async Task SendMessageAsync(string clientId, string message, CancellationToken cancellationToken = default)
        {
            await _connectionManager.SendMessageAsync(clientId, message, cancellationToken);
        }

        /// <summary>
        /// Broadcast a message to all clients connected to this server instance.
        /// (Does not go through broker).
        /// </summary>
        public async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            await _connectionManager.BroadcastAsync(message, cancellationToken);
        }

        /// <summary>
        /// Returns a snapshot of all connected clients and their sockets.
        /// </summary>
        public IDictionary<string, WebSocket> GetAllSockets()
        {
            return _connectionManager
                .GetAllIds()
                .Select(id => new { Id = id, Socket = _connectionManager.GetSocket(id) })
                .Where(x => x.Socket != null)
                .ToDictionary(x => x.Id, x => x.Socket!);
        }
    }
}
