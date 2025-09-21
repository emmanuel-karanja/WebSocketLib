using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using WebSocketUtils.Brokers;
using WebSocketUtils.Connection;
using Xunit;

namespace WebSocketUtils.Tests
{
    public class BrokeredConnectionManagerTests
    {
        private BrokeredConnectionManager CreateManager(
            out Mock<IMessageBroker> brokerMock,
            out Mock<ILogger<BrokeredConnectionManager>> loggerMock)
        {
            brokerMock = new Mock<IMessageBroker>();
            loggerMock = new Mock<ILogger<BrokeredConnectionManager>>();
            return new BrokeredConnectionManager(brokerMock.Object, loggerMock.Object);
        }

        [Fact]
        public async Task HandleConnectionAsync_AddsSocket_AndSubscribesToBroker()
        {
            // Arrange
            var manager = CreateManager(out var brokerMock, out var loggerMock);
            var socketMock = new Mock<WebSocket>();
            socketMock.Setup(s => s.State).Returns(WebSocketState.Open);

            // Fix: Use WebSocketReceiveResult, not ValueWebSocketReceiveResult
            socketMock.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

            // Act
            await manager.HandleConnectionAsync("client1", socketMock.Object, CancellationToken.None);

            // Assert
            brokerMock.Verify(b => b.SubscribeAsync("broadcast", It.IsAny<Func<string, Task>>()), Times.Once);
            Assert.DoesNotContain("client1", manager.GetAllIds()); // socket removed on Close
        }

        [Fact]
        public async Task SendMessageAsync_SendsData_WhenSocketOpen()
        {
            // Arrange
            var manager = CreateManager(out var brokerMock, out var loggerMock);
            var socketMock = new Mock<WebSocket>();
            socketMock.Setup(s => s.State).Returns(WebSocketState.Open);
            socketMock.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            manager.AddSocket("client1", socketMock.Object);

            // Act
            await manager.SendMessageAsync("client1", "hello");

            // Assert
            socketMock.Verify(s => s.SendAsync(
                It.Is<ArraySegment<byte>>(seg => Encoding.UTF8.GetString(seg) == "hello"),
                WebSocketMessageType.Text,
                true,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BroadcastMessageAsync_SendsMessageToAllSockets()
        {
            var manager = CreateManager(out var brokerMock, out var loggerMock);

            var socket1 = new Mock<WebSocket>();
            var socket2 = new Mock<WebSocket>();
            socket1.Setup(s => s.State).Returns(WebSocketState.Open);
            socket2.Setup(s => s.State).Returns(WebSocketState.Open);
            socket1.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
            socket2.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

            manager.AddSocket("c1", socket1.Object);
            manager.AddSocket("c2", socket2.Object);

            await manager.BroadcastMessageAsync("test", CancellationToken.None);

            socket1.Verify(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Once);
            socket2.Verify(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GetAllSockets_ReturnsDictionaryOfSockets()
        {
            var manager = CreateManager(out var brokerMock, out var loggerMock);
            var socket = new Mock<WebSocket>();
            socket.Setup(s => s.State).Returns(WebSocketState.Open);

            manager.AddSocket("c1", socket.Object);

            var allSockets = manager.GetAllSockets();

            Assert.True(allSockets.ContainsKey("c1"));
            Assert.Equal(socket.Object, allSockets["c1"]);
        }
    }
}
