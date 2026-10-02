using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Messages.Traffic.Analytics;
using Messages.Traffic.Light;
using TrafficLightCoordinatorStore.Publishers.Logs;
using TrafficLightCoordinatorStore.Publishers.Schedule;
using TrafficLightData;
using Microsoft.EntityFrameworkCore;

namespace TrafficLightCoordinatorStore.Aggregators.Analytics;

public class AnalyticsModeAggregator : IAnalyticsModeAggregator
{
    private readonly TrafficLightDbContext _db;
    private readonly ITrafficLightSchedulePublisher _schedulePublisher;
    private readonly ICoordinatorLogPublisher _log;
    private readonly ILogger<AnalyticsModeAggregator> _logger;

    public AnalyticsModeAggregator(
        TrafficLightDbContext db,
        ITrafficLightSchedulePublisher schedulePublisher,
        ICoordinatorLogPublisher log,
        ILogger<AnalyticsModeAggregator> logger)
    {
        _db = db;
        _schedulePublisher = schedulePublisher;
        _log = log;
        _logger = logger;
    }

    // ======================================================
    // Handle congestion analytics
    // ======================================================
    public async Task HandleCongestionAsync(CongestionAnalyticsMessage msg)
    {
        var mode = DetermineMode(msg);
        await PublishScheduleAsync(msg.Intersection, mode, "Congestion Analytics");
    }

    // ======================================================
    // Handle summary analytics
    // ======================================================
    public async Task HandleSummaryAsync(SummaryAnalyticsMessage msg)
    {
        var mode = DetermineMode(msg);
        await PublishScheduleAsync(msg.Intersection, mode, "Summary Analytics");
    }

    // ======================================================
    // Handle incident analytics
    // ======================================================
    public async Task HandleIncidentAsync(IncidentAnalyticsMessage msg)
    {
        var mode = DetermineMode(msg);
        await PublishScheduleAsync(msg.Intersection, mode, "Incident Analytics");
    }

    // ======================================================
    // Mode determination logic
    // ======================================================
    private static string DetermineMode(CongestionAnalyticsMessage msg)
    {
        return msg.Status.ToLower() switch
        {
            "heavy" => "Peak",
            "moderate" => "Standard",
            _ => "Standard"
        };
    }

    private static string DetermineMode(SummaryAnalyticsMessage msg)
    {
        if (msg.IncidentsDetected > 0) return "Incident";

        if (msg.AverageCongestion >= 0.75) return "Peak";
        if (msg.AverageCongestion >= 0.4) return "Standard";

        return "Standard";
    }

    private static string DetermineMode(IncidentAnalyticsMessage msg)
    {
        return msg.IncidentType.ToLower() switch
        {
            "collision" => "Incident",
            "obstruction" => "Incident",
            _ => "Standard"
        };
    }

    // ======================================================
    // Publish schedule via injected publisher + log audit
    // ======================================================
    private async Task PublishScheduleAsync(string intersectionName, string mode, string source)
    {
        // Fetch configuration from DB
        var config = await _db.TrafficConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Mode == mode)
            ?? await _db.TrafficConfigurations.FirstAsync(c => c.Mode == "Standard");

        var schedule = new TrafficLightScheduleMessage
        {
            IntersectionId = await GetIntersectionIdAsync(intersectionName),
            IntersectionName = intersectionName,
            CurrentMode = config.Mode,
            IsOperational = true,
            CycleDurationSec = config.CycleDurationSec,
            GlobalOffsetSec = config.GlobalOffsetSec,
            PhaseDurations = JsonSerializer.Deserialize<Dictionary<string, int>>(config.PhaseDurationsJson) ?? new(),
            Purpose = config.Purpose,
            LastUpdate = DateTime.UtcNow
        };

        // Publish to intersection controller
        await _schedulePublisher.PublishUpdateAsync(
            schedule.IntersectionId,
            schedule.IntersectionName!,
            schedule.IsOperational,
            schedule.CurrentMode,
            schedule.PhaseDurations,
            schedule.CycleDurationSec,
            schedule.GlobalOffsetSec,
            schedule.Purpose
        );

        // Audit log
        await _log.PublishAuditAsync(
            "TrafficCoordinator",
            $"Schedule published for {intersectionName} in mode {config.Mode}",
            "[ANALYTICS]",
            new Dictionary<string, object>
            {
            ["Intersection"] = intersectionName,
            ["Mode"] = config.Mode,
            ["Source"] = source
            },
            "mode-change");

        _logger.LogInformation("[AGGREGATOR][ANALYTICS] Published schedule for {Intersection} in mode {Mode}", intersectionName, config.Mode);
    }

    // ======================================================
    // Helper: fetch intersection ID by name
    // ======================================================
    private async Task<int> GetIntersectionIdAsync(string intersectionName)
    {
        var intersection = await _db.Intersections
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Name == intersectionName);

        if (intersection == null)
        {
            _logger.LogWarning("[AGGREGATOR][ANALYTICS] Intersection '{Intersection}' not found in DB", intersectionName);
            return 0; // fallback
        }

        return intersection.IntersectionId;
    }
}
