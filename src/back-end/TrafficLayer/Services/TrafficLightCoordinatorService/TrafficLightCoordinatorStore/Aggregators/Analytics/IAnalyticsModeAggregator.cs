using System;
using Messages.Traffic.Analytics;

namespace TrafficLightCoordinatorStore.Aggregators.Analytics;

public interface IAnalyticsModeAggregator
{
    Task HandleCongestionAsync(CongestionAnalyticsMessage msg);
    Task HandleSummaryAsync(SummaryAnalyticsMessage msg);
    Task HandleIncidentAsync(IncidentAnalyticsMessage msg);
}
