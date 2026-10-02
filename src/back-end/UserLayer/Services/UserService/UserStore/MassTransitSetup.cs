using MassTransit;
using RabbitMQ.Client;
using Messages.Log;
using Messages.User;

namespace UserStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddUserServiceMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");

                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                // Exchanges
                var userExchange = rabbit["Exchanges:User"] ?? "USER.EXCHANGE";
                var logExchange = rabbit["Exchanges:Log"] ?? "LOG.EXCHANGE";

                // Configure messages to be published
                cfg.Message<UserNotificationRequest>(m => m.SetEntityName(userExchange));
                cfg.Publish<UserNotificationRequest>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);
            });
        });

        return services;
    }
}
