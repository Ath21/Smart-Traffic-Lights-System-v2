using System;
using IntersectionControllerStore.Aggregators.Priority;
using MassTransit;
using Messages.Sensor.Detection;

namespace IntersectionControllerStore.Consumers;

public class IncidentDetectedConsumer : IConsumer<IncidentDetectedMessage>
{
    private readonly IPriorityAggregator _aggregator;
    private readonly ILogger<IncidentDetectedConsumer> _logger;

    public IncidentDetectedConsumer(IPriorityAggregator aggregator, ILogger<IncidentDetectedConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IncidentDetectedMessage> context)
    {
        _logger.LogInformation("[CONSUMER][INCIDENT] IncidentDetectedMessage received for intersection {Intersection}", context.Message.Intersection);
        await _aggregator.BuildPriorityEventAsync(context.Message);
    }
}
