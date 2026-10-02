using System;
using IntersectionControllerStore.Publishers.Logs;
using IntersectionControllerStore.Publishers.Priority;
using Messages.Sensor.Count;
using Messages.Sensor.Detection;
using Messages.Traffic.Priority;

namespace IntersectionControllerStore.Aggregators.Priority;

public class PriorityAggregator : IPriorityAggregator
{
    private readonly IPriorityPublisher _priorityPublisher;
    private readonly IIntersectionLogPublisher _logPublisher;
    private readonly ILogger<PriorityAggregator> _logger;

    public PriorityAggregator(
        IPriorityPublisher priorityPublisher,
        IIntersectionLogPublisher logPublisher,
        ILogger<PriorityAggregator> logger)
    {
        _priorityPublisher = priorityPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    // ============================================================
    // EMERGENCY VEHICLE
    // ============================================================
    public async Task<PriorityEventMessage?> BuildPriorityEventAsync(EmergencyVehicleDetectedMessage ev)
    {
        var message = new PriorityEventMessage
        {
            IntersectionId = ev.IntersectionId,
            IntersectionName = ev.Intersection,
            EventType = "EmergencyVehicle",
            VehicleType = ev.EmergencyVehicleType,
            PriorityLevel = 3,
            Direction = ev.Direction
        };

        await _priorityPublisher.PublishPriorityEventAsync(message);

        await _logPublisher.PublishAuditAsync(
            "EmergencyVehicle",
            $"High-priority emergency vehicle detected at {ev.Intersection}",
            "EmergencyVehicle",
            new Dictionary<string, object>
            {
            ["Direction"] = ev.Direction ?? "Unknown",
            ["VehicleType"] = ev.EmergencyVehicleType ?? "Unknown"
            },
            ev.CorrelationId
        );

        return message;
    }

    // ============================================================
    // INCIDENT
    // ============================================================
    public async Task<PriorityEventMessage?> BuildPriorityEventAsync(IncidentDetectedMessage incident)
    {
        var severity = incident.Metadata != null && incident.Metadata.TryGetValue("Severity", out var s)
            ? Convert.ToInt32(s)
            : 2;

        var message = new PriorityEventMessage
        {
            IntersectionId = incident.IntersectionId,
            IntersectionName = incident.Intersection,
            EventType = "Incident",
            PriorityLevel = severity,
            Direction = null
        };

        await _priorityPublisher.PublishPriorityEventAsync(message);

        await _logPublisher.PublishAuditAsync(
            "IncidentDetected",
            $"Incident reported at {incident.Intersection}",
            data: new() { ["Description"] = incident.Description ?? "N/A", ["Severity"] = severity },
            operation: incident.CorrelationId
        );

        return message;
    }

    // ============================================================
    // PUBLIC TRANSPORT
    // ============================================================
    public async Task<PriorityEventMessage?> BuildPriorityEventAsync(PublicTransportDetectedMessage pt)
    {
        var message = new PriorityEventMessage
        {
            IntersectionId = pt.IntersectionId,
            IntersectionName = pt.IntersectionName,
            EventType = "PublicTransport",
            VehicleType = pt.LineName,
            PriorityLevel = 2
        };

        await _priorityPublisher.PublishPriorityEventAsync(message);

        await _logPublisher.PublishAuditAsync(
            "PublicTransportDetected",
            $"Public transport detected at {pt.IntersectionName} ({pt.LineName ?? "Unknown line"})",
            data: new() { ["DetectedAt"] = pt.DetectedAt, ["LineName"] = pt.LineName ?? "N/A" },
            operation: pt.CorrelationId
        );

        return message;
    }

    // ============================================================
    // VEHICLE COUNT
    // ============================================================
    public async Task<PriorityCountMessage?> BuildPriorityCountAsync(VehicleCountMessage vc)
    {
        var priority = vc.CountTotal > 50 ? 3 :
                       vc.CountTotal > 20 ? 2 : 1;

        var isThresholdExceeded = vc.CountTotal > 20;

        var message = new PriorityCountMessage
        {
            IntersectionId = vc.IntersectionId,
            IntersectionName = vc.Intersection,
            CountType = "Vehicle",
            TotalCount = vc.CountTotal,
            PriorityLevel = priority,
            IsThresholdExceeded = isThresholdExceeded
        };

        await _priorityPublisher.PublishPriorityCountAsync(message);

        await _logPublisher.PublishAuditAsync(
            "VehicleCount",
            $"Vehicle count processed at {vc.Intersection} ({vc.CountTotal} vehicles)",
            data: new()
            {
            ["PriorityLevel"] = priority,
            ["ThresholdExceeded"] = isThresholdExceeded,
            ["AverageSpeedKmh"] = vc.AverageSpeedKmh,
            ["AverageWaitTimeSec"] = vc.AverageWaitTimeSec
            },
            operation: vc.CorrelationId
        );

        return message;
    }

    // ============================================================
    // PEDESTRIAN COUNT
    // ============================================================
    public async Task<PriorityCountMessage?> BuildPriorityCountAsync(PedestrianCountMessage pc)
    {
        var priority = pc.Count > 30 ? 3 :
                       pc.Count > 10 ? 2 : 1;

        var message = new PriorityCountMessage
        {
            IntersectionId = pc.IntersectionId,
            IntersectionName = pc.Intersection,
            CountType = "Pedestrian",
            TotalCount = pc.Count,
            PriorityLevel = priority,
            IsThresholdExceeded = pc.Count > 10
        };

        await _priorityPublisher.PublishPriorityCountAsync(message);

        await _logPublisher.PublishAuditAsync(
            "PedestrianCount",
            $"Pedestrian count processed at {pc.Intersection} ({pc.Count} pedestrians)",
            data: new()
            {
            ["PriorityLevel"] = priority,
            ["ThresholdExceeded"] = pc.Count > 10
            },
            operation: pc.CorrelationId
        );

        return message;
    }

    // ============================================================
    // CYCLIST COUNT
    // ============================================================
    public async Task<PriorityCountMessage?> BuildPriorityCountAsync(CyclistCountMessage cc)
    {
        var priority = cc.Count > 15 ? 3 :
                       cc.Count > 5 ? 2 : 1;

        var message = new PriorityCountMessage
        {
            IntersectionId = cc.IntersectionId,
            IntersectionName = cc.Intersection,
            CountType = "Cyclist",
            TotalCount = cc.Count,
            PriorityLevel = priority,
            IsThresholdExceeded = cc.Count > 5
        };

        await _priorityPublisher.PublishPriorityCountAsync(message);

        await _logPublisher.PublishAuditAsync(
            "CyclistCount",
            $"Cyclist count processed at {cc.Intersection} ({cc.Count} cyclists)",
            data: new Dictionary<string, object>
            {
            ["PriorityLevel"] = priority,
            ["ThresholdExceeded"] = cc.Count > 5
            },
            operation: cc.CorrelationId
        );

        return message;
    }
}
