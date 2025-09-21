using StackExchange.Redis;
namespace WebSocketUtils.Brokers
{
    public class RedisBroker : IMessageBroker
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _sub;

        public RedisBroker(string connectionString)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _sub = _redis.GetSubscriber();
        }

        public async Task PublishAsync(string channel, string message)
        {
            await _sub.PublishAsync(channel, message);
        }

        public async Task SubscribeAsync(string channel, Func<string, Task> handler)
        {
            await _sub.SubscribeAsync(channel, async (ch, value) => await handler(value));
        }
    }
}
