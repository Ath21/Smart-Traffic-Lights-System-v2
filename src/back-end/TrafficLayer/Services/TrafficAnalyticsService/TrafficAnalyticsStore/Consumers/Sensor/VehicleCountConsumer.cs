using MassTransit;
using Messages.Sensor.Count;
using TrafficAnalytics.Publishers.Logs;
using TrafficAnalyticsStore.Aggregators;
using TrafficAnalyticsStore.Business.DailySummary;
using TrafficAnalyticsStore.Publishers.Analytics;
using Microsoft.Extensions.Logging;
using TrafficAnalyticsStore.Aggregators.Analytics;
using Messages.Traffic.Analytics;

namespace TrafficAnalyticsStore.Consumers.Sensor;

public class VehicleCountConsumer : IConsumer<VehicleCountMessage>
{
    private readonly IDailySummaryBusiness _summaryBusiness;
    private readonly ITrafficAnalyticsAggregator _aggregation;
    private readonly ITrafficAnalyticsPublisher _analyticsPublisher;
    private readonly IAnalyticsLogPublisher _logPublisher;
    private readonly ILogger<VehicleCountConsumer> _logger;

    public VehicleCountConsumer(
        IDailySummaryBusiness summaryBusiness,
        ITrafficAnalyticsAggregator aggregation,
        ITrafficAnalyticsPublisher analyticsPublisher,
        IAnalyticsLogPublisher logPublisher,
        ILogger<VehicleCountConsumer> logger)
    {
        _summaryBusiness = summaryBusiness;
        _aggregation = aggregation;
        _analyticsPublisher = analyticsPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VehicleCountMessage> context)
    {
        var msg = context.Message;

        await _logPublisher.PublishAuditAsync(
            "[CONSUMER][VEHICLE_COUNT]",
            $"Received vehicle count {msg.CountTotal} at {msg.Intersection}",
            category: "DATA_PROCESSING");

        // 1️⃣ Update daily summary
        var summary = await _summaryBusiness.GetOrCreateTodayAsync(msg.IntersectionId, msg.Intersection);
        await _summaryBusiness.UpdateCountsAsync(summary, "Vehicle", msg.CountTotal, msg.AverageSpeedKmh, msg.AverageWaitTimeSec);

        // 2️⃣ Aggregate vehicle data
        _aggregation.AddVehicleData(msg);

        // 3️⃣ Publish congestion immediately
        var congestionLevel = _aggregation.ComputeCongestion(msg.AverageSpeedKmh, msg.AverageWaitTimeSec, msg.CountTotal);
        var congestionMsg = new CongestionAnalyticsMessage
        {
            Intersection = msg.Intersection,
            CongestionLevel = congestionLevel,
            Status = congestionLevel >= 0.7 ? "heavy" : congestionLevel >= 0.4 ? "moderate" : "normal",
            Timestamp = DateTime.UtcNow
        };
        await _analyticsPublisher.PublishCongestionAsync(congestionMsg);

        // 4️⃣ Optionally generate summary
        var summaryMsg = _aggregation.TryGenerateSummary(msg.IntersectionId);
        if (summaryMsg != null)
            await _analyticsPublisher.PublishSummaryAsync(summaryMsg);
    }

}
