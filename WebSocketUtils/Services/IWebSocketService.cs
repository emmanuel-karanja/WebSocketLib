using System.Threading;
using System.Threading.Tasks;

namespace WebSocketUtils.Services
{
    public interface IWebSocketService
    {
        Task HandleMessageAsync(string clientId, string messageText, CancellationToken cancellationToken);
    }
}
