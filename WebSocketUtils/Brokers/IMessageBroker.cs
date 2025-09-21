namespace WebSocketUtils.Brokers
{
    public interface IMessageBroker
    {
        Task PublishAsync(string channel, string message);
        Task SubscribeAsync(string channel, Func<string, Task> handler);
    }
}
