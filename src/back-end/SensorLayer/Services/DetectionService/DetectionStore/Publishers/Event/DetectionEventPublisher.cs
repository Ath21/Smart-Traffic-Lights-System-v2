using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Messages.Sensor.Detection;
using DetectionStore.Domain; 

namespace DetectionStore.Publishers.Event;

public class DetectionEventPublisher : IDetectionEventPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<DetectionEventPublisher> _logger;
    private readonly IntersectionContext _intersection;
    private readonly string _routingPattern;
    private const string domain = "[PUBLISHER][DETECTION]";

    public DetectionEventPublisher(
        IBus bus,
        IConfiguration config,
        ILogger<DetectionEventPublisher> logger,
        IntersectionContext intersection)
    {
        _bus = bus;
        _logger = logger;
        _intersection = intersection;
        _routingPattern = config["RabbitMQ:RoutingKeys:Sensor:DetectionEvent"]
                          ?? "sensor.detection.{intersection}.{event}";
    }

    public async Task PublishEmergencyVehicleDetectedAsync(EmergencyVehicleDetectedMessage message)
    {
        var routingKey = _routingPattern
            .Replace("{intersection}", _intersection.Name.ToLower().Replace(' ', '-'))
            .Replace("{event}", "emergency-vehicle");

        message.CorrelationId ??= Guid.NewGuid().ToString();
        message.DetectedAt = DateTime.UtcNow;
        message.IntersectionId = _intersection.Id;
        message.Intersection = _intersection.Name;

        await _bus.Publish(message, ctx => ctx.SetRoutingKey(routingKey));
        _logger.LogInformation("{Domain} Emergency vehicle detected at {Intersection} ({Type})\n",
            domain, _intersection.Name, message.EmergencyVehicleType);
    }

    public async Task PublishPublicTransportDetectedAsync(PublicTransportDetectedMessage message)
    {
        var routingKey = _routingPattern
            .Replace("{intersection}", _intersection.Name.ToLower().Replace(' ', '-'))
            .Replace("{event}", "public-transport");

        message.CorrelationId ??= Guid.NewGuid().ToString();
        message.DetectedAt = DateTime.UtcNow;
        message.IntersectionId = _intersection.Id;
        message.IntersectionName = _intersection.Name;

        await _bus.Publish(message, ctx => ctx.SetRoutingKey(routingKey));
        _logger.LogInformation("{Domain} Public transport detected at {Intersection} (Line={Line})\n",
            domain, _intersection.Name, message.LineName);
    }

    public async Task PublishIncidentDetectedAsync(IncidentDetectedMessage message)
    {
        var routingKey = _routingPattern
            .Replace("{intersection}", _intersection.Name.ToLower().Replace(' ', '-'))
            .Replace("{event}", "incident");

        message.CorrelationId ??= Guid.NewGuid().ToString();
        message.ReportedAt = DateTime.UtcNow;
        message.IntersectionId = _intersection.Id;
        message.Intersection = _intersection.Name;

        await _bus.Publish(message, ctx => ctx.SetRoutingKey(routingKey));
        _logger.LogInformation("{Domain} Incident detected at {Intersection}: {Description}\n",
            domain, _intersection.Name, message.Description);
    }
}
