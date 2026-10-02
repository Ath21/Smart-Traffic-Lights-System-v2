using MassTransit;
using Messages.Sensor.Detection;
using Messages.Traffic.Analytics;
using TrafficAnalytics.Publishers.Logs;
using TrafficAnalyticsStore.Aggregators.Analytics;
using TrafficAnalyticsStore.Business.Alerts;
using TrafficAnalyticsStore.Publishers.Analytics;

namespace TrafficAnalyticsStore.Consumers.Detection;

public class PublicTransportDetectedConsumer : IConsumer<PublicTransportDetectedMessage>
{
    private readonly IAlertBusiness _alertBusiness;
    private readonly ITrafficAnalyticsAggregator _aggregation;
    private readonly ITrafficAnalyticsPublisher _analyticsPublisher;
    private readonly IAnalyticsLogPublisher _logPublisher;
    private readonly ILogger<PublicTransportDetectedConsumer> _logger;

    public PublicTransportDetectedConsumer(
        IAlertBusiness alertBusiness,
        ITrafficAnalyticsAggregator aggregation,
        ITrafficAnalyticsPublisher analyticsPublisher,
        IAnalyticsLogPublisher logPublisher,
        ILogger<PublicTransportDetectedConsumer> logger)
    {
        _alertBusiness = alertBusiness;
        _aggregation = aggregation;
        _analyticsPublisher = analyticsPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PublicTransportDetectedMessage> context)
    {
        var msg = context.Message;

        await _logPublisher.PublishAuditAsync(
            "[CONSUMER][PUBLIC_TRANSPORT]",
            $"Public transport detected at {msg.IntersectionName}: Line {msg.LineName}",
            category: "ALERT_PROCESSING");

        // Persist alert
        if (!string.IsNullOrWhiteSpace(msg.LineName))
        {
            var alert = await _alertBusiness.CreateAlertAsync(
                msg.IntersectionId,
                msg.IntersectionName,
                "PublicTransport",
                $"Line {msg.LineName} detected",
                congestionIndex: 0,
                severity: 1);

            // Publish analytics as incident
            var analyticsMsg = new IncidentAnalyticsMessage
            {
                Intersection = msg.IntersectionName,
                IncidentType = $"PublicTransport: {msg.LineName}",
                Severity = alert.Severity,
                Timestamp = DateTime.UtcNow
            };
            await _analyticsPublisher.PublishIncidentAsync(analyticsMsg);
        }

        // Aggregate for summary
        _aggregation.AddPublicTransportDetection(msg);
    }
}

