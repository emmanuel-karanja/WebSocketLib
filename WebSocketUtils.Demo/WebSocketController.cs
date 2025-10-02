using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocketUtils.Services;
using WebSocketUtils.Connection;

namespace WebSocketUtils.Demo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly IWebSocketService _notificationService;
        private readonly ILogger<WebSocketController> _logger;
        private readonly ConnectionManager _connectionManager;

        private const int MAX_CONNECTIONS_PER_IP = 5; // example limit

        public WebSocketController(
            IWebSocketService notificationService,
            ConnectionManager connectionManager,
            ILogger<WebSocketController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("ws")]
        public async Task Get()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Limit connections per IP
            var currentConnections = _connectionManager.GetConnectionsByIp(ip).Count;
            if (currentConnections >= MAX_CONNECTIONS_PER_IP)
            {
                _logger.LogWarning("Connection limit reached for IP {IP}", ip);
                HttpContext.Response.StatusCode = 429; // Too Many Requests
                return;
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var clientId = Guid.NewGuid().ToString();
            var cancellationToken = HttpContext.RequestAborted;

            _logger.LogInformation("Client connected: {ClientId} from IP {IP}", clientId, ip);

            // Register socket in ConnectionManager with IP info
            _connectionManager.AddSocket(clientId, webSocket, ip);

            // Delegate lifecycle to NotificationWebService
            await _notificationService.HandleConnectionAsync(clientId, webSocket, cancellationToken);
        }
    }
}
