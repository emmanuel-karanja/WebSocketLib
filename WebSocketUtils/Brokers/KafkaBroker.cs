using Confluent.Kafka;

namespace WebSocketUtils.Brokers
{
    public class KafkaBroker : IMessageBroker
    {
        private readonly string _bootstrapServers;

        public KafkaBroker(string bootstrapServers)
        {
            _bootstrapServers = bootstrapServers;
        }

        public async Task PublishAsync(string channel, string message)
        {
            var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
            using var producer = new ProducerBuilder<Null, string>(config).Build();
            await producer.ProduceAsync(channel, new Message<Null, string> { Value = message });
        }

        public async Task SubscribeAsync(string channel, Func<string, Task> handler)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = $"{channel}-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(channel);

            while (true)
            {
                var cr = consumer.Consume();
                await handler(cr.Message.Value);
            }
        }
    }
}
