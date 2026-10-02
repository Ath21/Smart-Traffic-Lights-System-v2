using System;
using IntersectionControllerStore.Aggregators.Priority;
using MassTransit;
using Messages.Sensor.Count;

namespace IntersectionControllerStore.Consumers;

public class CyclistCountConsumer : IConsumer<CyclistCountMessage>
{
    private readonly IPriorityAggregator _aggregator;
    private readonly ILogger<CyclistCountConsumer> _logger;

    public CyclistCountConsumer(IPriorityAggregator aggregator, ILogger<CyclistCountConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CyclistCountMessage> context)
    {
        _logger.LogInformation("[CONSUMER][CYCLIST_COUNT] CyclistCountMessage received for intersection {Intersection}", context.Message.Intersection);
        await _aggregator.BuildPriorityCountAsync(context.Message);
    }
}
