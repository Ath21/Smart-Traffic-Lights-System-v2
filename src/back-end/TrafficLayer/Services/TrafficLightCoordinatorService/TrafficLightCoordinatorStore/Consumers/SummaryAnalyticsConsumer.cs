using System;
using MassTransit;
using Messages.Traffic.Analytics;
using TrafficLightCoordinatorStore.Aggregators.Analytics;

namespace TrafficLightCoordinatorStore.Consumers;

public class SummaryAnalyticsConsumer : IConsumer<SummaryAnalyticsMessage>
{
    private readonly IAnalyticsModeAggregator _aggregator;
    private readonly ILogger<SummaryAnalyticsConsumer> _logger;

    public SummaryAnalyticsConsumer(
        IAnalyticsModeAggregator aggregator,
        ILogger<SummaryAnalyticsConsumer> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SummaryAnalyticsMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[CONSUMER][ANALYTICS][{Intersection}] Received summary analytics (Vehicles={Vehicles}, Pedestrians={Pedestrians}, Incidents={Incidents})",
            msg.Intersection, msg.VehicleCount, msg.PedestrianCount, msg.IncidentsDetected);

        await _aggregator.HandleSummaryAsync(msg);
    }
}
