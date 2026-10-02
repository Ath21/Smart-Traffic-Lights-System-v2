using MassTransit;
using Messages.Log;
using Messages.Sensor.Count;
using RabbitMQ.Client;

namespace SensorStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddSensorServiceMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");
                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h => { h.Username(rabbit["Username"]); h.Password(rabbit["Password"]); });

                var sensorExchange = rabbit["Exchanges:Sensor"];
                var logExchange = rabbit["Exchanges:Log"];

                cfg.Message<VehicleCountMessage>(m => m.SetEntityName(sensorExchange));
                cfg.Message<PedestrianCountMessage>(m => m.SetEntityName(sensorExchange));
                cfg.Message<CyclistCountMessage>(m => m.SetEntityName(sensorExchange));
                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));

                cfg.Publish<VehicleCountMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<PedestrianCountMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<CyclistCountMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
