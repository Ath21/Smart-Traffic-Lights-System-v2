using MassTransit;
using Messages.Sensor.Detection;
using TrafficAnalytics.Publishers.Logs;
using TrafficAnalyticsStore.Aggregators;
using TrafficAnalyticsStore.Business.DailySummary;
using TrafficAnalyticsStore.Publishers.Analytics;
using Microsoft.Extensions.Logging;
using TrafficAnalyticsStore.Aggregators.Analytics;
using TrafficAnalyticsStore.Business.Alerts;
using Messages.Traffic.Analytics;

namespace TrafficAnalyticsStore.Consumers.Detection;

public class EmergencyVehicleDetectedConsumer : IConsumer<EmergencyVehicleDetectedMessage>
{
    private readonly IAlertBusiness _alertBusiness;
    private readonly ITrafficAnalyticsAggregator _aggregation;
    private readonly ITrafficAnalyticsPublisher _analyticsPublisher;
    private readonly IAnalyticsLogPublisher _logPublisher;
    private readonly ILogger<EmergencyVehicleDetectedConsumer> _logger;

    public EmergencyVehicleDetectedConsumer(
        IAlertBusiness alertBusiness,
        ITrafficAnalyticsAggregator aggregation,
        ITrafficAnalyticsPublisher analyticsPublisher,
        IAnalyticsLogPublisher logPublisher,
        ILogger<EmergencyVehicleDetectedConsumer> logger)
    {
        _alertBusiness = alertBusiness;
        _aggregation = aggregation;
        _analyticsPublisher = analyticsPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmergencyVehicleDetectedMessage> context)
    {
        var msg = context.Message;

        await _logPublisher.PublishAuditAsync(
            "[CONSUMER][EMERGENCY_VEHICLE]",
            $"Emergency vehicle detected at {msg.Intersection}: {msg.EmergencyVehicleType} heading {msg.Direction}",
            category: "ALERT_PROCESSING");

        // Persist alert
        var alert = await _alertBusiness.CreateAlertAsync(
            msg.IntersectionId,
            msg.Intersection,
            "EmergencyVehicle",
            $"{msg.EmergencyVehicleType} detected heading {msg.Direction}",
            congestionIndex: 0,
            severity: 2);

        // Aggregate for summary
        _aggregation.AddEmergencyVehicleDetection(msg);

        // Publish analytics as incident
        var analyticsMsg = new IncidentAnalyticsMessage
        {
            Intersection = msg.Intersection,
            IncidentType = msg.EmergencyVehicleType ?? "EmergencyVehicle",
            Severity = alert.Severity,
            Timestamp = DateTime.UtcNow
        };
        await _analyticsPublisher.PublishIncidentAsync(analyticsMsg);
    }
}



