using MassTransit;
using RabbitMQ.Client;
using LogStore.Consumers;
using Messages.Log;

namespace LogStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddLogServiceMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<LogConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");
                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                var logExchange     = rabbit["Exchanges:Log"];
                var logUserQueue    = rabbit["Queues:Log:User"];
                var logTrafficQueue = rabbit["Queues:Log:Traffic"];
                var logSensorQueue  = rabbit["Queues:Log:Sensor"];

                // Dynamic routing patterns
                var rkUserPattern    = rabbit["RoutingKeys:Log:User"];
                var rkTrafficPattern = rabbit["RoutingKeys:Log:Traffic"];
                var rkSensorPattern  = rabbit["RoutingKeys:Log:Sensor"];

                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // -----------------------------
                // USER Logs Queue
                // -----------------------------
                cfg.ReceiveEndpoint(logUserQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind(logExchange, s =>
                    {
                        s.RoutingKey = rkUserPattern;
                        s.ExchangeType = ExchangeType.Topic;
                    });
                    e.ConfigureConsumer<LogConsumer>(context);
                    e.PrefetchCount = 10;
                });

                // -----------------------------
                // TRAFFIC Logs Queue
                // -----------------------------
                cfg.ReceiveEndpoint(logTrafficQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind(logExchange, s =>
                    {
                        s.RoutingKey = rkTrafficPattern;
                        s.ExchangeType = ExchangeType.Topic;
                    });
                    e.ConfigureConsumer<LogConsumer>(context);
                    e.PrefetchCount = 10;
                });

                // -----------------------------
                // SENSOR Logs Queue
                // -----------------------------
                cfg.ReceiveEndpoint(logSensorQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind(logExchange, s =>
                    {
                        s.RoutingKey = rkSensorPattern;
                        s.ExchangeType = ExchangeType.Topic;
                    });
                    e.ConfigureConsumer<LogConsumer>(context);
                    e.PrefetchCount = 10;
                });
            });
        });

        return services;
    }
}
