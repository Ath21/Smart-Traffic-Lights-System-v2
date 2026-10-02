using System;
using MassTransit;
using Messages.Traffic.Analytics;
using TrafficLightCoordinatorStore.Aggregators.Analytics;

namespace TrafficLightCoordinatorStore.Consumers;

public class CongestionAnalyticsConsumer : IConsumer<CongestionAnalyticsMessage>
{
    private readonly IAnalyticsModeAggregator _aggregator;
    private readonly ILogger<CongestionAnalyticsConsumer> _logger;

    public CongestionAnalyticsConsumer(
        IAnalyticsModeAggregator aggregator,
        ILogger<CongestionAnalyticsConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CongestionAnalyticsMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[CONSUMER][ANALYTICS][{Intersection}] Received congestion analytics (Level={Level}, Status={Status})",
            msg.Intersection, msg.CongestionLevel, msg.Status);

        await _aggregator.HandleCongestionAsync(msg);
    }
}