using MassTransit;
using Messages.Sensor.Count;
using TrafficAnalytics.Publishers.Logs;
using TrafficAnalyticsStore.Aggregators;
using TrafficAnalyticsStore.Business.DailySummary;
using TrafficAnalyticsStore.Publishers.Analytics;
using Microsoft.Extensions.Logging;
using TrafficAnalyticsStore.Aggregators.Analytics;

namespace TrafficAnalyticsStore.Consumers.Sensor;

public class PedestrianCountConsumer : IConsumer<PedestrianCountMessage>
{
    private readonly IDailySummaryBusiness _summaryBusiness;
    private readonly ITrafficAnalyticsAggregator _aggregation;
    private readonly ITrafficAnalyticsPublisher _analyticsPublisher;
    private readonly IAnalyticsLogPublisher _logPublisher;
    private readonly ILogger<PedestrianCountConsumer> _logger;

    public PedestrianCountConsumer(
        IDailySummaryBusiness summaryBusiness,
        ITrafficAnalyticsAggregator aggregation,
        ITrafficAnalyticsPublisher analyticsPublisher,
        IAnalyticsLogPublisher logPublisher,
        ILogger<PedestrianCountConsumer> logger)
    {
        _summaryBusiness = summaryBusiness;
        _aggregation = aggregation;
        _analyticsPublisher = analyticsPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PedestrianCountMessage> context)
    {
        var msg = context.Message;

        await _logPublisher.PublishAuditAsync(
            "[CONSUMER][PEDESTRIAN_COUNT]",
            $"Received pedestrian count {msg.Count} at {msg.Intersection}",
            category: "DATA_PROCESSING");

        // 1️⃣ Update daily summary (only count available)
        var summary = await _summaryBusiness.GetOrCreateTodayAsync(msg.IntersectionId, msg.Intersection);
        await _summaryBusiness.UpdateCountsAsync(summary, "Pedestrian", msg.Count, 0, 0);

        // 2️⃣ Add to in-memory aggregation
        _aggregation.AddPedestrianData(msg);

        // 3️⃣ Try to produce and publish a summary
        var summaryMsg = _aggregation.TryGenerateSummary(msg.IntersectionId);
        if (summaryMsg != null)
            await _analyticsPublisher.PublishSummaryAsync(summaryMsg);
    }
}
