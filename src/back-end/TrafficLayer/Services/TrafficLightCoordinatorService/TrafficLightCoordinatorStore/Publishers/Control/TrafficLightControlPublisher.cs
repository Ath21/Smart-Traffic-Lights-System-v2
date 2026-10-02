using System;
using MassTransit;
using Messages.Traffic.Light;

namespace TrafficLightCoordinatorStore.Publishers.Control;


public class TrafficLightControlPublisher : ITrafficLightControlPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<TrafficLightControlPublisher> _logger;
    private readonly string _routingPattern;

    public TrafficLightControlPublisher(IBus bus, IConfiguration config, ILogger<TrafficLightControlPublisher> logger)
    {
        _bus = bus;
        _logger = logger;

        // Configurable routing pattern (default: traffic.light.control.{intersection}.{light})
        _routingPattern = config["RabbitMQ:RoutingKeys:Traffic:LightControl"]
                          ?? "traffic.light.control.{intersection}.{light}";
    }

    public async Task PublishControlAsync(
        int intersectionId,
        string intersectionName,
        int lightId,
        string lightName,
        string operationalMode,
        Dictionary<string, int> phaseDurations,
        int remainingTimeSec,
        int cycleDurationSec,
        int localOffsetSec,
        double cycleProgressSec,
        int priorityLevel,
        bool isFailoverActive,
        string? timePlan = null)
    {
        var msg = new TrafficLightControlMessage
        {
            IntersectionId = intersectionId,
            IntersectionName = intersectionName,
            LightId = lightId,
            LightName = lightName,
            Mode = operationalMode,
            OperationalMode = operationalMode,
            TimePlan = timePlan,
            PhaseDurations = phaseDurations,
            RemainingTimeSec = remainingTimeSec,
            CycleDurationSec = cycleDurationSec,
            LocalOffsetSec = localOffsetSec,
            CycleProgressSec = cycleProgressSec,
            PriorityLevel = priorityLevel,
            IsFailoverActive = isFailoverActive,
            LastUpdate = DateTime.UtcNow
        };

        var routingKey = _routingPattern
            .Replace("{intersection}", intersectionName.ToLower().Replace(' ', '-'))
            .Replace("{light}", lightName.ToLower().Replace(' ', '-'));

        await _bus.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation(
            "[PUBLISHER][CONTROL][{Intersection}.{Light}] Applied override (Mode={Mode}, Priority={Priority}, Failover={Failover})",
            intersectionName, lightName, operationalMode, priorityLevel, isFailoverActive);
    }
}
