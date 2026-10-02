using System;
using MassTransit;
using Messages.Traffic.Priority;
using TrafficLightCoordinatorStore.Aggregators.Priority;

namespace TrafficLightCoordinatorStore.Consumers;


public class PriorityEventConsumer : IConsumer<PriorityEventMessage>
{
    private readonly ITrafficModeAggregator _aggregator;
    private readonly ILogger<PriorityEventConsumer> _logger;

    public PriorityEventConsumer(ITrafficModeAggregator aggregator, ILogger<PriorityEventConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PriorityEventMessage> context)
    {
        _logger.LogInformation("[CONSUMER][PRIORITY_EVENT] Received PriorityEventMessage for {Intersection}", context.Message.IntersectionName);
        await _aggregator.HandlePriorityEventAsync(context.Message);
    }
}