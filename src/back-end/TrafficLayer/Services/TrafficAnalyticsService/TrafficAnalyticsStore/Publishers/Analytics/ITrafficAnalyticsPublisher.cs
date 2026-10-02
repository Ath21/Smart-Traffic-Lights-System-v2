using System;
using Messages.Traffic.Analytics;

namespace TrafficAnalyticsStore.Publishers.Analytics;

public interface ITrafficAnalyticsPublisher
{
    Task PublishCongestionAsync(CongestionAnalyticsMessage message);
    Task PublishIncidentAsync(IncidentAnalyticsMessage message);
    Task PublishSummaryAsync(SummaryAnalyticsMessage message);
}

