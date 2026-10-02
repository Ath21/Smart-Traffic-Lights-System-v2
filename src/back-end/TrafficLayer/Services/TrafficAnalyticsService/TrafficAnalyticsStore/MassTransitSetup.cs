using MassTransit;
using RabbitMQ.Client;
using Messages.Log;
using Messages.Traffic.Analytics;
using TrafficAnalyticsStore.Consumers;
using TrafficAnalyticsStore.Consumers.Sensor;
using TrafficAnalyticsStore.Consumers.Detection;

namespace TrafficAnalyticsStore;

public static class MassTransitSetup
{
    public static IServiceCollection AddTrafficAnalyticsMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // =====================================================
            // Register Consumers
            // =====================================================
            x.AddConsumer<VehicleCountConsumer>();
            x.AddConsumer<PedestrianCountConsumer>();
            x.AddConsumer<CyclistCountConsumer>();

            x.AddConsumer<EmergencyVehicleDetectedConsumer>();
            x.AddConsumer<IncidentDetectedConsumer>();
            x.AddConsumer<PublicTransportDetectedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMQ");

                // ===============================
                // Connection Setup
                // ===============================
                cfg.Host(rabbit["Host"], rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbit["Username"]);
                    h.Password(rabbit["Password"]);
                });

                // =====================================================
                // Exchanges
                // =====================================================
                var sensorExchange  = rabbit["Exchanges:Sensor"]  ?? "SENSOR.EXCHANGE";
                var trafficExchange = rabbit["Exchanges:Traffic"] ?? "TRAFFIC.EXCHANGE";
                var logExchange     = rabbit["Exchanges:Log"]     ?? "LOG.EXCHANGE";

                // =====================================================
                // Queues
                // =====================================================
                var sensorQueue    = rabbit["Queues:Sensor:SensorCount"]     ?? "sensor-2-analytics.queue";
                var detectionQueue = rabbit["Queues:Sensor:DetectionEvents"] ?? "detection-2-analytics.queue";

                // =====================================================
                // Routing Keys
                // =====================================================
                var sensorCountPattern = rabbit["RoutingKeys:Sensor:SensorCount"] ?? "sensor.count.#"; 
                var detectionEventPattern = rabbit["RoutingKeys:Sensor:DetectionEvents"]   ?? "sensor.detection.#";

                // =====================================================
                // [PUBLISH] TRAFFIC ANALYTICS (Congestion, Incident, Summary)
                // =====================================================
                cfg.Message<CongestionAnalyticsMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Message<IncidentAnalyticsMessage>(m => m.SetEntityName(trafficExchange));
                cfg.Message<SummaryAnalyticsMessage>(m => m.SetEntityName(trafficExchange));

                cfg.Publish<CongestionAnalyticsMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<IncidentAnalyticsMessage>(m => m.ExchangeType = ExchangeType.Topic);
                cfg.Publish<SummaryAnalyticsMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // =====================================================
                // [PUBLISH] LOGS (AUDIT, ERROR)
                // =====================================================
                cfg.Message<LogMessage>(m => m.SetEntityName(logExchange));
                cfg.Publish<LogMessage>(m => m.ExchangeType = ExchangeType.Topic);

                // =====================================================
                // [CONSUME] SENSOR QUEUE
                // =====================================================
                //
                // Routing pattern: sensor.count.{intersection}.{count}
                // Example: sensor.count.kentriki-pyli.vehicle
                //
                cfg.ReceiveEndpoint(sensorQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;

                    e.Bind(sensorExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = sensorCountPattern;
                    });

                    e.ConfigureConsumer<VehicleCountConsumer>(context);
                    e.ConfigureConsumer<PedestrianCountConsumer>(context);
                    e.ConfigureConsumer<CyclistCountConsumer>(context);

                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // =====================================================
                // [CONSUME] DETECTION QUEUE
                // =====================================================
                //
                // Routing pattern: sensor.detection.{intersection}.{event}
                // Example: sensor.detection.agiou-spyridonos.emergency-vehicle
                //
                cfg.ReceiveEndpoint(detectionQueue, e =>
                {
                    e.ConfigureConsumeTopology = false;

                    e.Bind(sensorExchange, s =>
                    {
                        s.ExchangeType = ExchangeType.Topic;
                        s.RoutingKey = detectionEventPattern;
                    });

                    e.ConfigureConsumer<EmergencyVehicleDetectedConsumer>(context);
                    e.ConfigureConsumer<IncidentDetectedConsumer>(context);
                    e.ConfigureConsumer<PublicTransportDetectedConsumer>(context);

                    e.PrefetchCount = 10;
                    e.ConcurrentMessageLimit = 5;
                });

                // =====================================================
                // DONE
                // =====================================================
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
