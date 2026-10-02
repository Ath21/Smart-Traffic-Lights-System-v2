using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Messages.Traffic.Priority;
using TrafficLightCoordinatorStore.Publishers.Schedule;
using TrafficLightCoordinatorStore.Publishers.Logs;
using TrafficLightData;

namespace TrafficLightCoordinatorStore.Aggregators.Priority;

public class TrafficModeAggregator : ITrafficModeAggregator
{
    private readonly TrafficLightDbContext _db;
    private readonly ITrafficLightSchedulePublisher _schedulePublisher;
    private readonly ICoordinatorLogPublisher _log;
    private readonly ILogger<TrafficModeAggregator> _logger;

    public TrafficModeAggregator(
        TrafficLightDbContext db,
        ITrafficLightSchedulePublisher schedulePublisher,
        ICoordinatorLogPublisher log,
        ILogger<TrafficModeAggregator> logger)
    {
        _db = db;
        _schedulePublisher = schedulePublisher;
        _log = log;
        _logger = logger;
    }

    // ============================================================
    // HANDLE PRIORITY COUNT
    // ============================================================
    public async Task HandlePriorityCountAsync(PriorityCountMessage msg)
    {
        var mode = DetermineMode(msg);
        await PublishScheduleAsync(msg.IntersectionId, msg.IntersectionName, mode, "Count-based evaluation");
    }

    // ============================================================
    // HANDLE PRIORITY EVENT
    // ============================================================
    public async Task HandlePriorityEventAsync(PriorityEventMessage msg)
    {
        var mode = DetermineMode(msg);
        await PublishScheduleAsync(msg.IntersectionId, msg.IntersectionName, mode, "Event-based evaluation");
    }

    // ============================================================
    // DETERMINE MODE
    // ============================================================
    private static string DetermineMode(PriorityCountMessage msg)
    {
        if (msg.IsThresholdExceeded)
        {
            return msg.CountType?.ToLower() switch
            {
                "vehicle" => "Peak",
                "pedestrian" => "Pedestrian",
                "cyclist" => "Cyclist",
                _ => "Standard"
            };
        }
        return "Standard";
    }

    private static string DetermineMode(PriorityEventMessage msg) =>
        msg.EventType?.ToLower() switch
        {
            "emergency-vehicle" => "Emergency",
            "public-transport" => "PublicTransport",
            "incident" => "Incident",
            _ => "Standard"
        };

    // ============================================================
    // PUBLISH NEW TRAFFIC SCHEDULE
    // ============================================================
    private async Task PublishScheduleAsync(int intersectionId, string? name, string mode, string source)
    {
        var config = await _db.TrafficConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Mode == mode)
            ?? await _db.TrafficConfigurations.FirstAsync(c => c.Mode == "Standard");

        var phaseDurations = JsonSerializer.Deserialize<Dictionary<string, int>>(config.PhaseDurationsJson) ?? new();

        await _schedulePublisher.PublishUpdateAsync(
            intersectionId: intersectionId,
            intersectionName: name ?? $"intersection-{intersectionId}",
            isOperational: true,
            currentMode: config.Mode,
            phaseDurations: phaseDurations,
            cycleDurationSec: config.CycleDurationSec,
            globalOffsetSec: config.GlobalOffsetSec,
            purpose: config.Purpose
        );

        await _log.PublishAuditAsync(
            domain: "[AGGREGATOR][TRAFFIC_MODE]",
            messageText: $"Schedule published for {name ?? intersectionId.ToString()} in mode {config.Mode}",
            category: "mode-change",
            data: new Dictionary<string, object>
            {
            ["IntersectionId"] = intersectionId,
            ["Mode"] = config.Mode,
            ["Source"] = source
            },
            operation: "PublishSchedule"
        );

        _logger.LogInformation(
            "[AGGREGATOR][TRAFFIC_MODE] Published schedule for {Intersection} in mode {Mode}",
            name ?? intersectionId.ToString(),
            config.Mode);
    }
}
