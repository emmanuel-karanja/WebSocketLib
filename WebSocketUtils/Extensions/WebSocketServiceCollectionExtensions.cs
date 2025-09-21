using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSocketUtils.Brokers;
using WebSocketUtils.Connection;
using WebSocketUtils.Demo.Options; // Now this will exist
namespace WebSocketUtils.Extensions
{
    public static class WebSocketServiceCollectionExtensions
    {
        /// <summary>
        /// Adds WebSocket brokers and a brokered connection manager to the DI container.
        /// </summary>
        public static IServiceCollection AddWebSocketBrokers(this IServiceCollection services,
         Action<BrokerOptions>? configureOptions = null)
        {
            // Configure options
            var options = new BrokerOptions();
            configureOptions?.Invoke(options);

            // Add brokers
            services.AddSingleton<IMessageBroker>(sp =>
            {
                // Currently using RedisBroker by default, extendable
                return new RedisBroker($"{options.RedisHost}:{options.RedisPort}");
            });

            // Add BrokeredConnectionManager
            services.AddSingleton<BrokeredConnectionManager>(sp =>
            {
                var broker = sp.GetRequiredService<IMessageBroker>();
                var logger = sp.GetRequiredService<ILogger<BrokeredConnectionManager>>();
                return new BrokeredConnectionManager(broker, logger);
            });

            return services;
        }
    }
}
