using MassTransit;
using RabbitMQ.Client;
using IntersectionControllerStore.Consumers;
using IntersectionControllerStore.Consumers.Light;
using Messages.Traffic;
using Messages.Log;
using Messages.Traffic.Priority;
using Messages.Traffic.Light;

namespace IntersectionControllerStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddIntersectionControllerMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // =====================================================
            // Register Consumers
            // =====================================================
            x.AddConsumer<LightScheduleConsumer>();
            x.AddConsumer<EmergencyVehicleDetectedConsumer>();
            x.AddConsumer<PublicTransportDetectedConsumer>();
            x.AddConsumer<IncidentDetectedConsumer>();
            x.AddConsumer<VehicleCountConsumer>();
            x.AddConsumer<PedestrianCountConsumer>();
            x.AddConsumer<CyclistCountConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");
                var intersection = configuration["Intersection:Name"]?.ToLower().Replace(" ", "-") ?? "default";

                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                // =====================================================
                // Exchanges
                // =====================================================
                var trafficExchange = rabbit["Exchanges:Traffic"];
                var sensorExchange  = rabbit["Exchanges:Sensor"];
                var logExchange     = rabbit["Exchanges:Log"];

                // =====================================================
                // Queues (intersection specific)
                // =====================================================
                var lightQueue     = rabbit["Queues:Traffic:LightSchedule"]?.Replace("{intersection}", intersection);
                var countQueue     = rabbit["Queues:Sensor:SensorCount"]?.Replace("{intersection}", intersection);
                var detectionQueue = rabbit["Queues:Sensor:DetectionEvents"]?.Replace("{intersection}", intersection);

                // =====================================================
                // Routing Keys
                // =====================================================
                var trafficLightKeys    = new[] { rabbit["RoutingKeys:Traffic:LightSchedule"] ?? "traffic.light.schedule.*" };
                var sensorCountKeys     = new[] { rabbit["RoutingKeys:Sensor:SensorCount"] ?? "sensor.count.#" };
                var sensorDetectionKeys = new[] { rabbit["RoutingKeys:Sensor:DetectionEvents"] ?? "sensor.detection.#" };

                // =====================================================
                // PUBLISH CONFIGURATION
                // =====================================================
                cfg.Message<TrafficLightControlMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Publish<TrafficLightControlMessage>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.Message<PriorityEventMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Publish<PriorityEventMessage>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.Message<PriorityCountMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Publish<PriorityCountMessage>(m => m.ExchangeType = ExchangeType.Topic);

                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // =====================================================
                // CONSUMERS / ENDPOINTS
                // =====================================================

                // LIGHT SCHEDULE
                cfg.ReceiveEndpoint(lightQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    foreach (var key in trafficLightKeys)
                        e.Bind(trafficExchange, s =>
                        {
                            s.ExchangeType = ExchangeType.Topic;
                            s.RoutingKey = key;
                        });

                    e.ConfigureConsumer<LightScheduleConsumer>(context);
                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // SENSOR COUNT
                cfg.ReceiveEndpoint(countQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    foreach (var key in sensorCountKeys)
                        e.Bind(sensorExchange, s =>
                        {
                            s.ExchangeType = ExchangeType.Topic;
                            s.RoutingKey = key;
                        });

                    e.ConfigureConsumer<VehicleCountConsumer>(context);
                    e.ConfigureConsumer<PedestrianCountConsumer>(context);
                    e.ConfigureConsumer<CyclistCountConsumer>(context);

                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // SENSOR DETECTION
                cfg.ReceiveEndpoint(detectionQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;
                    foreach (var key in sensorDetectionKeys)
                        e.Bind(sensorExchange, s =>
                        {
                            s.ExchangeType = ExchangeType.Topic;
                            s.RoutingKey = key;
                        });

                    e.ConfigureConsumer<EmergencyVehicleDetectedConsumer>(context);
                    e.ConfigureConsumer<PublicTransportDetectedConsumer>(context);
                    e.ConfigureConsumer<IncidentDetectedConsumer>(context);

                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
