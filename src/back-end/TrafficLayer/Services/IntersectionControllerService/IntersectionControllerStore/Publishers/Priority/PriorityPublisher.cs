using System;
using MassTransit;
using Messages.Traffic;
using Messages.Traffic.Priority;

namespace IntersectionControllerStore.Publishers.Priority;

public class PriorityPublisher : IPriorityPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<PriorityPublisher> _logger;
    private readonly string _routingPatternCount;
    private readonly string _routingPatternEvent;

    public PriorityPublisher(IBus bus, IConfiguration config, ILogger<PriorityPublisher> logger)
    {
        _bus = bus;
        _logger = logger;
        _routingPatternCount = config["RabbitMQ:RoutingKeys:Traffic:PriorityCount"]
                               ?? "priority.count.{intersection}.{count}";
        _routingPatternEvent = config["RabbitMQ:RoutingKeys:Traffic:PriorityEvent"]
                               ?? "priority.detection.{intersection}.{event}";
    }

    public async Task PublishPriorityCountAsync(PriorityCountMessage msg)
    {
        var routingKey = _routingPatternCount
            .Replace("{intersection}", msg.IntersectionName!.ToLower().Replace(' ', '-'))
            .Replace("{count}", msg.CountType.ToString().ToLower());

        await _bus.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation("[PUBLISHER][PRIORITY][COUNT][{Intersection}] {CountType}={Count}, Priority={Level}",
            msg.IntersectionName, msg.CountType, msg.TotalCount, msg.PriorityLevel);
    }

    public async Task PublishPriorityEventAsync(PriorityEventMessage msg)
    {
        var routingKey = _routingPatternEvent
            .Replace("{intersection}", msg.IntersectionName!.ToLower().Replace(' ', '-'))
            .Replace("{event}", msg.EventType.ToString().ToLower());

        await _bus.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation("[PUBLISHER][PRIORITY][EVENT][{Intersection}] {EventType} ({VehicleType}) Priority={Level}",
            msg.IntersectionName, msg.EventType, msg.VehicleType, msg.PriorityLevel);
        
        await _bus.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));

        _logger.LogInformation("[PUBLISHER][PRIORITY][EVENT][{Intersection}] {EventType} ({VehicleType}) Priority={Level}",
            msg.IntersectionName, msg.EventType, msg.VehicleType, msg.PriorityLevel);
    }
}