using System;
using MassTransit;
using Messages.Traffic.Priority;
using TrafficLightCoordinatorStore.Aggregators.Priority;

namespace TrafficLightCoordinatorStore.Consumers;

public class PriorityCountConsumer : IConsumer<PriorityCountMessage>
{
    private readonly ITrafficModeAggregator _aggregator;
    private readonly ILogger<PriorityCountConsumer> _logger;

    public PriorityCountConsumer(ITrafficModeAggregator aggregator, ILogger<PriorityCountConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PriorityCountMessage> context)
    {
        _logger.LogInformation("[CONSUMER][PRIORITY_COUNT] Received PriorityCountMessage for {Intersection}", context.Message.IntersectionName);
        await _aggregator.HandlePriorityCountAsync(context.Message);
    }
}