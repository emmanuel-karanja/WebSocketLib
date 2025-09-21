using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketUtils.Connection;
using WebSocketUtils.Demo.Services;

namespace WebSocketUtils.Demo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly BrokeredConnectionManager _manager;
        private readonly IWebSocketMessageService _messageHandler;  // 👈 use the interface
        private readonly ILogger<WebSocketController> _logger;

        public WebSocketController(
            BrokeredConnectionManager manager,
            IWebSocketMessageService messageHandler,  // 👈 inject interface
            ILogger<WebSocketController> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/websocket/ws
        [HttpGet("ws")]
        public async Task Get()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var clientId = Guid.NewGuid().ToString();
            var cancellationToken = HttpContext.RequestAborted;

            _logger.LogInformation("Client connected: {ClientId}", clientId);

            _manager.AddSocket(clientId, webSocket);

            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _manager.RemoveSocketAsync(clientId);
                        _logger.LogInformation("Client disconnected: {ClientId}", clientId);
                        break;
                    }

                    var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Message from {ClientId}: {Message}", clientId, messageText);

                    await _messageHandler.HandleMessageAsync(clientId, messageText, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection cancelled for {ClientId}", clientId);
                await _manager.RemoveSocketAsync(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket for {ClientId}", clientId);
                await _manager.RemoveSocketAsync(clientId);
            }
        }
    }
}
