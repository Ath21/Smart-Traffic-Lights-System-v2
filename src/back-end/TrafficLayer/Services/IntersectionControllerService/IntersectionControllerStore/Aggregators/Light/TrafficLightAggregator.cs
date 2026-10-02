using System;
using System.Collections.Generic;
using IntersectionControllerStore.Domain;
using IntersectionControllerStore.Publishers.Light;
using IntersectionControllerStore.Publishers.Logs;
using Messages.Traffic.Light;
using Microsoft.Extensions.Logging;

namespace IntersectionControllerStore.Aggregators.Light;

public class TrafficLightAggregator : ITrafficLightAggregator
{
    private readonly ITrafficLightPublisher _lightPublisher;
    private readonly IIntersectionLogPublisher _logPublisher;
    private readonly ILogger<TrafficLightAggregator> _logger;

    public TrafficLightAggregator(
        ITrafficLightPublisher lightPublisher,
        IIntersectionLogPublisher logPublisher,
        ILogger<TrafficLightAggregator> logger)
    {
        _lightPublisher = lightPublisher;
        _logPublisher = logPublisher;
        _logger = logger;
    }

    public async Task<TrafficLightControlMessage> BuildLightControlAsync(
        TrafficLightScheduleMessage schedule,
        TrafficLightContext light,
        int lightIndex = 0,
        int totalLights = 1)
    {
        // Calculate per-light offset
        int cycleDuration = schedule.CycleDurationSec > 0 ? schedule.CycleDurationSec : 60;
        int baseOffset = schedule.GlobalOffsetSec;
        int staggerOffset = (totalLights > 1)
            ? (cycleDuration / totalLights) * lightIndex
            : 0;

        int localOffset = (baseOffset + staggerOffset) % cycleDuration;

        var control = new TrafficLightControlMessage
        {
            IntersectionId = schedule.IntersectionId,
            IntersectionName = schedule.IntersectionName,
            LightId = light.Id,
            LightName = light.Name,
            Mode = schedule.CurrentMode,
            OperationalMode = schedule.IsOperational ? "Normal" : "Off",
            PhaseDurations = schedule.PhaseDurations,
            CycleDurationSec = schedule.CycleDurationSec,
            LocalOffsetSec = localOffset,
            CurrentPhase = "Green",
            RemainingTimeSec = schedule.PhaseDurations.ContainsKey("Green") ? schedule.PhaseDurations["Green"] : 0,
            PriorityLevel = 1,
            IsFailoverActive = false,
            LastUpdate = DateTime.UtcNow
        };

        // Publish to light controller
        await _lightPublisher.PublishLightControlAsync(control);

        // Log the assignment
        await _logPublisher.PublishAuditAsync(
            "LightScheduleApplied",
            $"Applied light schedule for light {light.Name} (Intersection {schedule.IntersectionName})",
            category: "system",
            data: new Dictionary<string, object>
            {
                ["LightId"] = light.Id,
                ["Mode"] = schedule.CurrentMode,
                ["CycleDurationSec"] = schedule.CycleDurationSec,
                ["LocalOffsetSec"] = localOffset,
                ["Operational"] = schedule.IsOperational
            });

        _logger.LogInformation(
            "[Intersection {Intersection}] Sent control to light {Light} ({Mode}) with offset {Offset}s",
            schedule.IntersectionName,
            light.Name,
            schedule.CurrentMode,
            localOffset);

        return control;
    }
}
