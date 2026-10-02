using System;
using IntersectionControllerStore.Aggregators.Priority;
using MassTransit;
using Messages.Sensor.Detection;

namespace IntersectionControllerStore.Consumers;


public class EmergencyVehicleDetectedConsumer : IConsumer<EmergencyVehicleDetectedMessage>
{
    private readonly IPriorityAggregator _aggregator;
    private readonly ILogger<EmergencyVehicleDetectedConsumer> _logger;

    public EmergencyVehicleDetectedConsumer(IPriorityAggregator aggregator, ILogger<EmergencyVehicleDetectedConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmergencyVehicleDetectedMessage> context)
    {
        _logger.LogInformation("[CONSUMER][EMERGENCY_VEHICLE] EmergencyVehicleDetectedMessage received for intersection {Intersection}", context.Message.Intersection);
        await _aggregator.BuildPriorityEventAsync(context.Message);
    }
}