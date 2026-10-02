using MassTransit;
using RabbitMQ.Client;
using Messages.Log;
using Messages.Traffic.Light;
using TrafficLightCoordinatorStore.Consumers;

namespace TrafficLightCoordinatorStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddTrafficCoordinatorMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // =====================================================
            // Register Consumers
            // =====================================================
            x.AddConsumer<PriorityEventConsumer>();
            x.AddConsumer<PriorityCountConsumer>();
            x.AddConsumer<CongestionAnalyticsConsumer>();
            x.AddConsumer<IncidentAnalyticsConsumer>();
            x.AddConsumer<SummaryAnalyticsConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");

                // =====================================================
                // Connection
                // =====================================================
                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                // =====================================================
                // Exchanges
                // =====================================================
                var trafficExchange = rabbit["Exchanges:Traffic"];
                var logExchange     = rabbit["Exchanges:Log"];

                // =====================================================
                // Queues
                // =====================================================
                var priorityCountQueue     = rabbit["Queues:Traffic:PriorityCount"];
                var priorityDetectionQueue = rabbit["Queues:Traffic:PriorityDetection"];
                var analyticsQueue         = rabbit["Queues:Traffic:Analytics"];

                // =====================================================
                // Routing Keys (use wildcards for intersection)
                // =====================================================
                var priorityCountKey     = rabbit["RoutingKeys:Traffic:PriorityCount"];
                var priorityDetectionKey = rabbit["RoutingKeys:Traffic:PriorityDetection"];
                var analyticsKey         = rabbit["RoutingKeys:Traffic:Analytics"];

                // =====================================================
                // [PUBLISH] TRAFFIC LIGHT SCHEDULE
                // =====================================================
                cfg.Message<TrafficLightScheduleMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Publish<TrafficLightScheduleMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // =====================================================
                // [PUBLISH] LOGS
                // =====================================================
                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // =====================================================
                // [CONSUME] PRIORITY DETECTION EVENTS
                // =====================================================
                cfg.ReceiveEndpoint(priorityDetectionQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind(trafficExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = priorityDetectionKey;
                    });

                    e.ConfigureConsumer<PriorityEventConsumer>(context);
                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // =====================================================
                // [CONSUME] PRIORITY COUNT EVENTS
                // =====================================================
                cfg.ReceiveEndpoint(priorityCountQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind(trafficExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = priorityCountKey;
                    });

                    e.ConfigureConsumer<PriorityCountConsumer>(context);
                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // =====================================================
                // [CONSUME] TRAFFIC ANALYTICS METRICS
                // =====================================================
                cfg.ReceiveEndpoint(analyticsQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind(trafficExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = analyticsKey;
                    });

                    e.ConfigureConsumer<CongestionAnalyticsConsumer>(context);
                    e.ConfigureConsumer<IncidentAnalyticsConsumer>(context);
                    e.ConfigureConsumer<SummaryAnalyticsConsumer>(context);

                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
