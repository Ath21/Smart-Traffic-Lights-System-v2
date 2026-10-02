using MassTransit;
using RabbitMQ.Client;
using NotificationStore.Consumers;
using Messages.User;
using Messages.Traffic.Analytics;
using Messages.Log;

namespace NotificationStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddNotificationServiceMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<UserNotificationRequestConsumer>();
            x.AddConsumer<IncidentAnalyticsConsumer>();
            x.AddConsumer<CongestionAnalyticsConsumer>();
            x.AddConsumer<SummaryAnalyticsConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");

                // Core connection
                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                // Exchanges
                var userExchange   = rabbit["Exchanges:User"];
                var logExchange    = rabbit["Exchanges:Log"];
                var trafficExchange = rabbit["Exchanges:Traffic"];

                // Queues
                var userQueue          = rabbit["Queues:User:NotificationRequests"];
                var trafficAnalyticsQ  = rabbit["Queues:Traffic:Analytics"];

                // Routing keys
                var userNotifKeyPattern  = rabbit["RoutingKeys:User:Notifications"];
                var trafficAnalyticKey   = rabbit["RoutingKeys:Traffic:Analytics"];

                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);
                
                // [1] User → Notification requests
                cfg.ReceiveEndpoint(userQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.ConfigureConsumer<UserNotificationRequestConsumer>(context);

                    e.Bind(userExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = userNotifKeyPattern;
                    });

                    e.PrefetchCount = 10;
                });

                // [2] Traffic → Analytics (Incident, Congestion, Summary)
                cfg.ReceiveEndpoint(trafficAnalyticsQ, e =>
                {
                    e.ConfigureConsumeTopology = false;

                    e.Bind(trafficExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = trafficAnalyticKey;
                    });
                    
                    e.ConfigureConsumer<IncidentAnalyticsConsumer>(context);
                    e.ConfigureConsumer<CongestionAnalyticsConsumer>(context);
                    e.ConfigureConsumer<SummaryAnalyticsConsumer>(context);
                    
                    e.PrefetchCount = 20;
                });
            });
        });

        return services;
    }
}
