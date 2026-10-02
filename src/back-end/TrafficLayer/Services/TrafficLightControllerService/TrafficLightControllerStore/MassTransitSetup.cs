using MassTransit;
using RabbitMQ.Client;
using Messages.Log;
using Messages.Traffic;
using TrafficLightControllerStore.Consumers;
using Messages.Traffic.Light;

namespace TrafficLightControllerStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddTrafficLightControllerMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // =====================================================
            // Register Consumers
            // =====================================================
            x.AddConsumer<TrafficLightControlConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");

                // =====================================================
                // Common Configuration
                // =====================================================
                var intersection = configuration["Intersection:Name"]?.ToLower().Replace(" ", "-");
                var light = configuration["Light:Name"]?.ToLower().Replace(" ", "-");

                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                // =====================================================
                // Exchanges
                // =====================================================
                var trafficExchange = rabbit["Exchanges:Traffic"];
                var logExchange = rabbit["Exchanges:Log"];

                // =====================================================
                // Queues (intersection & light specific)
                // =====================================================
                var controlQueuePattern = rabbit["Queues:Traffic:LightControl"];
                var controlQueue = controlQueuePattern
                    ?.Replace("{intersection}", intersection)
                    ?.Replace("{light}", light);

                // =====================================================
                // Routing Keys
                // =====================================================
                var controlKeyPattern = rabbit["RoutingKeys:Traffic:LightControl"] ?? "traffic.light.control.#";
                var controlKeys = new[]
                {
                    controlKeyPattern
                        .Replace("{intersection}", intersection)
                        .Replace("{light}", light)
                };

                var logKeyPattern = rabbit["RoutingKeys:Log:LightController"] ?? "log.{layer}.{service}.{type}";

                // =====================================================
                // PUBLISH CONFIGURATION
                // =====================================================
                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.Message<TrafficLightControlMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Publish<TrafficLightControlMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // =====================================================
                // [CONSUME] TRAFFIC LIGHT CONTROL COMMANDS
                // =====================================================
                cfg.ReceiveEndpoint(controlQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;

                    foreach (var key in controlKeys)
                    {
                        e.Bind(trafficExchange, s =>
                        {
                            s.ExchangeType = ExchangeType.Topic;
                            s.RoutingKey = key;
                        });
                    }

                    e.ConfigureConsumer<TrafficLightControlConsumer>(context);
                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // =====================================================
                // FINALIZE
                // =====================================================
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
