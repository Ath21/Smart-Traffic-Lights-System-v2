using System;
using MassTransit;
using Messages.Traffic.Analytics;
using TrafficLightCoordinatorStore.Aggregators.Analytics;

namespace TrafficLightCoordinatorStore.Consumers;

public class IncidentAnalyticsConsumer : IConsumer<IncidentAnalyticsMessage>
{
    private readonly IAnalyticsModeAggregator _aggregator;
    private readonly ILogger<IncidentAnalyticsConsumer> _logger;

    public IncidentAnalyticsConsumer(
        IAnalyticsModeAggregator aggregator,
        ILogger<IncidentAnalyticsConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IncidentAnalyticsMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[CONSUMER][ANALYTICS][{Intersection}] Received incident analytics (Type={Type}, Severity={Severity})",
            msg.Intersection, msg.IncidentType, msg.Severity);

        await _aggregator.HandleIncidentAsync(msg);
    }
}