using MassTransit;
using Messages.Traffic.Light;
using TrafficLightCacheData.Repositories;
using TrafficLightControllerStore.Aggregators.Control;
using TrafficLightControllerStore.Publishers.Logs;

namespace TrafficLightControllerStore.Consumers;

public class TrafficLightControlConsumer : IConsumer<TrafficLightControlMessage>
{
    private readonly ITrafficLightCacheRepository _cache;
    private readonly ITrafficLightAggregator _aggregator;
    private readonly ITrafficLightLogPublisher _logPublisher;
    private readonly ILogger<TrafficLightControlConsumer> _logger;

    public TrafficLightControlConsumer(
        ITrafficLightCacheRepository cache,
        ITrafficLightAggregator aggregator,
        ITrafficLightLogPublisher logPublisher,
        ILogger<TrafficLightControlConsumer> logger)
    {
        _cache = cache;
        _aggregator = aggregator;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TrafficLightControlMessage> context)
    {
        var msg = context.Message;

        // =================================
        // Update aggregator (starts light timers)
        // =================================
        await _aggregator.ApplyControlMessageAsync(msg);

        // =================================
        // Optionally update cache for diagnostics / heartbeat
        // =================================
        await _cache.SetHeartbeatAsync(msg.IntersectionId, msg.LightId);
        await _cache.SetCoordinatorSyncAsync(msg.IntersectionId);

        // =================================
        // Logging / audit
        // =================================
        await _logPublisher.PublishAuditAsync(
            "TrafficLightControlApplied",
            $"Traffic light control applied for intersection {msg.IntersectionName}",
            data: new Dictionary<string, object>
            {
                ["LightName"] = msg.LightName,
                ["Mode"] = msg.Mode,
                ["OperationalMode"] = msg.OperationalMode,
                ["PriorityLevel"] = msg.PriorityLevel,
            },
            operation: context.CorrelationId?.ToString()
        );

        _logger.LogInformation(
            "Traffic light control applied: {Intersection}-{Light} Mode={Mode} OperationalMode={OpMode}",
            msg.IntersectionName,
            msg.LightName,
            msg.Mode,
            msg.OperationalMode
        );
    }
}
