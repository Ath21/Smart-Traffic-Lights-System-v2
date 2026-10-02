using MassTransit;
using Messages.Sensor.Detection;
using TrafficAnalyticsStore.Business.Alerts;
using TrafficAnalyticsStore.Publishers.Analytics;
using TrafficAnalytics.Publishers.Logs;
using Microsoft.Extensions.Logging;
using TrafficAnalyticsStore.Aggregators.Analytics;
using Messages.Traffic.Analytics;

namespace TrafficAnalyticsStore.Consumers.Detection;

public class IncidentDetectedConsumer : IConsumer<IncidentDetectedMessage>
{
    private readonly IAlertBusiness _alertBusiness;
    private readonly ITrafficAnalyticsAggregator _aggregation;
    private readonly ITrafficAnalyticsPublisher _analyticsPublisher;
    private readonly IAnalyticsLogPublisher _logPublisher;
    private readonly ILogger<IncidentDetectedConsumer> _logger;

    public IncidentDetectedConsumer(
        IAlertBusiness alertBusiness,
        ITrafficAnalyticsAggregator aggregation,
        ITrafficAnalyticsPublisher analyticsPublisher,
        IAnalyticsLogPublisher logPublisher,
        ILogger<IncidentDetectedConsumer> logger)
    {
        _alertBusiness = alertBusiness;
        _aggregation = aggregation;
        _analyticsPublisher = analyticsPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IncidentDetectedMessage> context)
    {
        var msg = context.Message;

        await _logPublisher.PublishAuditAsync(
            "[CONSUMER][INCIDENT_DETECTED]",
            $"Incident reported at {msg.Intersection}: {msg.Description}",
            category: "ALERT_PROCESSING");

        // Persist alert
        var alert = await _alertBusiness.CreateAlertAsync(
            msg.IntersectionId,
            msg.Intersection,
            "Incident",
            msg.Description ?? "No description",
            congestionIndex: 0,
            severity: 1);

        // Aggregate for summary
        _aggregation.AddIncidentDetection(msg);

        // Publish analytics
        var analyticsMsg = new IncidentAnalyticsMessage
        {
            Intersection = msg.Intersection,
            IncidentType = msg.Description ?? "Unknown",
            Severity = alert.Severity,
            Timestamp = DateTime.UtcNow
        };
        await _analyticsPublisher.PublishIncidentAsync(analyticsMsg);
    }
}

