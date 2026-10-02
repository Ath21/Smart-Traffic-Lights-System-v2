using System;
using IntersectionControllerStore.Aggregators.Priority;
using MassTransit;
using Messages.Sensor.Count;

namespace IntersectionControllerStore.Consumers;


public class VehicleCountConsumer : IConsumer<VehicleCountMessage>
{
    private readonly IPriorityAggregator _aggregator;
    private readonly ILogger<VehicleCountConsumer> _logger;

    public VehicleCountConsumer(IPriorityAggregator aggregator, ILogger<VehicleCountConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VehicleCountMessage> context)
    {
        _logger.LogInformation("[CONSUMER][VEHICLE_COUNT] VehicleCountMessage received for intersection {Intersection}", context.Message.Intersection);
        await _aggregator.BuildPriorityCountAsync(context.Message);
    }
}
