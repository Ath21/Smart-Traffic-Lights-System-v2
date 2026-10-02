using MassTransit;
using Messages;
using Messages.Log;
using Messages.Sensor;
using Messages.Sensor.Detection;
using RabbitMQ.Client;

namespace DetectionStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddDetectionServiceMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");
                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h => { h.Username(rabbit["Username"]); h.Password(rabbit["Password"]); });

                var sensorExchange = rabbit["Exchanges:Sensor"];
                var logExchange = rabbit["Exchanges:Log"];

                cfg.Message<EmergencyVehicleDetectedMessage>(m => m.SetEntityName(sensorExchange));
                cfg.Message<PublicTransportDetectedMessage>(m => m.SetEntityName(sensorExchange));
                cfg.Message<IncidentDetectedMessage>(m => m.SetEntityName(sensorExchange));
                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));

                cfg.Publish<EmergencyVehicleDetectedMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<PublicTransportDetectedMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<IncidentDetectedMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}