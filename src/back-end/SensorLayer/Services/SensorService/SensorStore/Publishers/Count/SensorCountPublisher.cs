using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Messages.Sensor.Count;
using SensorStore.Domain;

namespace SensorStore.Publishers.Count;

public class SensorCountPublisher : ISensorCountPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<SensorCountPublisher> _logger;
    private readonly IntersectionContext _intersection;
    private readonly string _routingPattern;

    public SensorCountPublisher(
        IConfiguration config,
        ILogger<SensorCountPublisher> logger,
        IBus bus,
        IntersectionContext intersection)
    {
        _bus = bus;
        _logger = logger;
        _intersection = intersection;

        _routingPattern = config["RabbitMQ:RoutingKeys:Sensor:SensorCount"]
                          ?? "sensor.count.{intersection}.{count}";
    }

    // ============================================================
    // VEHICLE COUNT
    // ============================================================
    public async Task PublishVehicleCountAsync(VehicleCountMessage message)
    {
        var routingKey = _routingPattern
            .Replace("{intersection}", _intersection.Name.ToLower().Replace(' ', '-'))
            .Replace("{count}", "vehicle");

        message.CorrelationId ??= Guid.NewGuid().ToString();
        message.Timestamp = DateTime.UtcNow;
        message.Intersection = _intersection.Name;
        message.IntersectionId = _intersection.Id;

        await _bus.Publish(message, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation(
            "[PUBLISHER][COUNT][{Intersection}] VEHICLE count published: {Count} (Speed={Speed:F1} km/h, Wait={Wait:F1}s, Flow={Flow:F2}/s)",
            _intersection.Name, message.CountTotal, message.AverageSpeedKmh, message.AverageWaitTimeSec, message.FlowRate);
    }

    // ============================================================
    // PEDESTRIAN COUNT
    // ============================================================
    public async Task PublishPedestrianCountAsync(PedestrianCountMessage message)
    {
        var routingKey = _routingPattern
            .Replace("{intersection}", _intersection.Name.ToLower().Replace(' ', '-'))
            .Replace("{count}", "pedestrian");

        message.CorrelationId ??= Guid.NewGuid().ToString();
        message.Timestamp = DateTime.UtcNow;
        message.Intersection = _intersection.Name;
        message.IntersectionId = _intersection.Id;

        await _bus.Publish(message, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation(
            "[PUBLISHER][COUNT][{Intersection}] PEDESTRIAN count published: {Count}",
            _intersection.Name, message.Count);
    }

    // ============================================================
    // CYCLIST COUNT
    // ============================================================
    public async Task PublishCyclistCountAsync(CyclistCountMessage message)
    {
        var routingKey = _routingPattern
            .Replace("{intersection}", _intersection.Name.ToLower().Replace(' ', '-'))
            .Replace("{count}", "cyclist");

        message.CorrelationId ??= Guid.NewGuid().ToString();
        message.Timestamp = DateTime.UtcNow;
        message.Intersection = _intersection.Name;
        message.IntersectionId = _intersection.Id;

        await _bus.Publish(message, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation(
            "[PUBLISHER][COUNT][{Intersection}] CYCLIST count published: {Count}",
            _intersection.Name, message.Count);
    }

}
