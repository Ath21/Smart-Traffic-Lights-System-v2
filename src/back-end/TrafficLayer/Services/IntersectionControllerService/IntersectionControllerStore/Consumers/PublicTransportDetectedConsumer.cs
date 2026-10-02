using System;
using IntersectionControllerStore.Aggregators.Priority;
using MassTransit;
using Messages.Sensor.Detection;

namespace IntersectionControllerStore.Consumers;


public class PublicTransportDetectedConsumer : IConsumer<PublicTransportDetectedMessage>
{
    private readonly IPriorityAggregator _aggregator;
    private readonly ILogger<PublicTransportDetectedConsumer> _logger;

    public PublicTransportDetectedConsumer(IPriorityAggregator aggregator, ILogger<PublicTransportDetectedConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PublicTransportDetectedMessage> context)
    {
        _logger.LogInformation("[CONSUMER][PUBLIC_TRANSPORT] PublicTransportDetectedMessage received for intersection {Intersection}", context.Message.IntersectionName);
        await _aggregator.BuildPriorityEventAsync(context.Message);
    }
}
