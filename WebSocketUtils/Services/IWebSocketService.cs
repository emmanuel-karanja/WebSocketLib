using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketUtils.Services
{
    /// <summary>
    /// Contract for services that handle WebSocket connections and messages.
    /// </summary>
    public interface IWebSocketService
    {
        /// <summary>
        /// Handles the lifecycle of a WebSocket connection:
        /// - Receiving messages
        /// - Closing socket gracefully on disconnect or errors
        /// </summary>
        /// <param name="clientId">Unique client identifier</param>
        /// <param name="webSocket">The WebSocket instance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task HandleConnectionAsync(string clientId, WebSocket webSocket, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles a single incoming message from a client.
        /// </summary>
        /// <param name="clientId">Unique client identifier</param>
        /// <param name="messageText">The raw message text</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task HandleMessageAsync(string clientId, string messageText, CancellationToken cancellationToken = default);
    }
}
