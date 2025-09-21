using System.Net.WebSockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketUtils.Connection
{
    public interface IConnectionManager
    {
        void AddSocket(string id, WebSocket socket);
        Task RemoveSocketAsync(string id);
        Task BroadcastAsync(string message, CancellationToken cancellationToken = default);
        Task SendMessageAsync(string clientId, string message, CancellationToken cancellationToken = default);
        WebSocket? GetSocket(string clientId);
        IEnumerable<string> GetAllIds();
    }
}
