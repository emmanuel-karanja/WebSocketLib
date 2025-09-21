using System.Threading;
using System.Threading.Tasks;

namespace WebSocketUtils.Demo.Services
{
    public interface IWebSocketMessageService
    {
        Task HandleMessageAsync(string clientId, string messageText, CancellationToken cancellationToken);
    }
}
