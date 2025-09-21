using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebSocketUtils.Connection;
using Xunit;

namespace WebSocketUtils.Tests
{
    public class ConnectionManagerTests
    {
        private ConnectionManager CreateManager(out Mock<ILogger<ConnectionManager>> loggerMock)
        {
            loggerMock = new Mock<ILogger<ConnectionManager>>();
            return new ConnectionManager(loggerMock.Object); // ✅ Compiles if ConnectionManager implements IConnectionManager
        }


        [Fact]
        public void AddSocket_ShouldStoreClientId()
        {
            var manager = CreateManager(out _);
            var socketMock = new Mock<WebSocket>();

            manager.AddSocket("client1", socketMock.Object);

            manager.GetAllIds().Should().Contain("client1");
        }

        [Fact]
        public void GetSocket_ShouldReturnCorrectSocket()
        {
            var manager = CreateManager(out _);
            var socketMock = new Mock<WebSocket>();

            manager.AddSocket("client1", socketMock.Object);
            var retrieved = manager.GetSocket("client1");

            retrieved.Should().Be(socketMock.Object);
        }

        [Fact]
        public async Task RemoveSocketAsync_ShouldCloseAndRemoveSocket()
        {
            var manager = CreateManager(out _);
            var socketMock = new Mock<WebSocket>();

            socketMock.Setup(s => s.State).Returns(WebSocketState.Open);
            socketMock
                .Setup(s => s.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                                         "Closed by manager",
                                         It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            manager.AddSocket("client1", socketMock.Object);

            await manager.RemoveSocketAsync("client1");

            manager.GetAllIds().Should().NotContain("client1");
            socketMock.Verify(s => s.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closed by manager",
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldSendMessageToClient()
        {
            var manager = CreateManager(out _);
            var socketMock = new Mock<WebSocket>();

            socketMock.Setup(s => s.State).Returns(WebSocketState.Open);
            socketMock
                .Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                                        WebSocketMessageType.Text,
                                        true,
                                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            manager.AddSocket("client1", socketMock.Object);

            await manager.SendMessageAsync("client1", "Hello");

            socketMock.Verify(s => s.SendAsync(
                It.Is<ArraySegment<byte>>(seg => Encoding.UTF8.GetString(seg) == "Hello"),
                WebSocketMessageType.Text,
                true,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastAsync_ShouldSendMessageToAllClients()
        {
            var manager = CreateManager(out _);
            var socket1 = new Mock<WebSocket>();
            var socket2 = new Mock<WebSocket>();

            socket1.Setup(s => s.State).Returns(WebSocketState.Open);
            socket2.Setup(s => s.State).Returns(WebSocketState.Open);

            socket1
                .Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                                        WebSocketMessageType.Text,
                                        true,
                                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            socket2
                .Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
                                        WebSocketMessageType.Text,
                                        true,
                                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            manager.AddSocket("client1", socket1.Object);
            manager.AddSocket("client2", socket2.Object);

            await manager.BroadcastAsync("Hello all");

            socket1.Verify(s => s.SendAsync(
                It.Is<ArraySegment<byte>>(seg => Encoding.UTF8.GetString(seg) == "Hello all"),
                WebSocketMessageType.Text,
                true,
                It.IsAny<CancellationToken>()),
                Times.Once);

            socket2.Verify(s => s.SendAsync(
                It.Is<ArraySegment<byte>>(seg => Encoding.UTF8.GetString(seg) == "Hello all"),
                WebSocketMessageType.Text,
                true,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
