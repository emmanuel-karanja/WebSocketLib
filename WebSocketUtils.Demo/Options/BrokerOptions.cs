namespace WebSocketUtils.Demo.Options
{
    public class BrokerOptions
    {
        public string RedisHost { get; set; } = "localhost";
        public int RedisPort { get; set; } = 6379;
        public string KafkaBootstrapServers { get; set; } = "";
    }
}
