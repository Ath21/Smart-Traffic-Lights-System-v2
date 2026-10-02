using System;
using Messages.Traffic.Light;
using Microsoft.EntityFrameworkCore;
using TrafficLightCoordinatorStore.Publishers.Control;
using TrafficLightCoordinatorStore.Publishers.Logs;
using TrafficLightCoordinatorStore.Publishers.Schedule;
using TrafficLightData;

namespace TrafficLightCoordinatorStore.Business.Operator;

public class TrafficOperatorBusiness : ITrafficOperatorBusiness
{
    private readonly TrafficLightDbContext _db;
    private readonly ITrafficLightSchedulePublisher _schedulePublisher;
    private readonly ITrafficLightControlPublisher _controlPublisher;
    private readonly ICoordinatorLogPublisher _log;
    private readonly ILogger<TrafficOperatorBusiness> _logger;

    public TrafficOperatorBusiness(
        TrafficLightDbContext db,
        ITrafficLightSchedulePublisher schedulePublisher,
        ITrafficLightControlPublisher controlPublisher,
        ICoordinatorLogPublisher log,
        ILogger<TrafficOperatorBusiness> logger)
    {
        _db = db;
        _schedulePublisher = schedulePublisher;
        _controlPublisher = controlPublisher;   
        _log = log;
        _logger = logger;
    }

    public async Task ApplyModeAsync(int intersectionId, string mode)
    {
        var intersection = await _db.Intersections
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IntersectionId == intersectionId);

        if (intersection == null)
        {
            _logger.LogWarning("Intersection {Id} not found", intersectionId);
            throw new KeyNotFoundException($"Intersection {intersectionId} not found");
        }

        var config = await _db.TrafficConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Mode == mode)
            ?? throw new KeyNotFoundException($"Mode '{mode}' not found in configurations");

        var schedule = new TrafficLightScheduleMessage
        {
            IntersectionId = intersection.IntersectionId,
            IntersectionName = intersection.Name,
            CurrentMode = config.Mode,
            IsOperational = true,
            CycleDurationSec = config.CycleDurationSec,
            GlobalOffsetSec = config.GlobalOffsetSec,
            PhaseDurations = System.Text.Json.JsonSerializer
                                .Deserialize<Dictionary<string, int>>(config.PhaseDurationsJson) ?? new(),
            Purpose = config.Purpose,
            LastUpdate = DateTime.UtcNow
        };

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

        await _log.PublishAuditAsync(
            "TrafficOperator",
            $"Manual mode applied for {intersection.Name}: {config.Mode}",
            "[TRAFFIC-OPERATOR]",
            new Dictionary<string, object>
            {
            ["IntersectionId"] = intersection.IntersectionId,
            ["Mode"] = config.Mode
            },
            "manual-mode");

        _logger.LogInformation("[TRAFFIC-OPERATOR] Applied mode {Mode} to intersection {Intersection}", config.Mode, intersection.Name);
    }

    public async Task OverrideLightAsync(
        int intersectionId,
        int lightId,
        string mode,
        Dictionary<string, int>? phaseDurations = null,
        int remainingTimeSec = 0,
        int cycleDurationSec = 60,
        int localOffsetSec = 0,
        double cycleProgressSec = 0,
        int priorityLevel = 1,
        bool isFailoverActive = false)
    {
        var intersection = await _db.Intersections
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IntersectionId == intersectionId);

        var light = await _db.TrafficLights
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.IntersectionId == intersectionId && l.LightId == lightId);

        if (intersection == null)
        {
            _logger.LogWarning("Intersection {Id} not found", intersectionId);
            throw new KeyNotFoundException($"Intersection {intersectionId} not found");
        }

        if (light == null)
        {
            _logger.LogWarning("Light {Id} not found on intersection {Intersection}", lightId, intersectionId);
            throw new KeyNotFoundException($"Light {lightId} not found on intersection {intersectionId}");
        }

        var controlMessage = new TrafficLightControlMessage
        {
            IntersectionId = intersection.IntersectionId,
            IntersectionName = intersection.Name,
            LightId = light.LightId,
            LightName = light.LightName,
            OperationalMode = mode,
            PhaseDurations = phaseDurations ?? new Dictionary<string, int> { { "Green", 30 }, { "Yellow", 5 }, { "Red", 25 } },
            RemainingTimeSec = remainingTimeSec,
            CycleDurationSec = cycleDurationSec,
            LocalOffsetSec = localOffsetSec,
            CycleProgressSec = cycleProgressSec,
            PriorityLevel = priorityLevel,
            IsFailoverActive = isFailoverActive,
            LastUpdate = DateTime.UtcNow
        };

        await _controlPublisher.PublishControlAsync(
            controlMessage.IntersectionId,
            controlMessage.IntersectionName!,
            controlMessage.LightId,
            controlMessage.LightName!,
            controlMessage.OperationalMode,
            controlMessage.PhaseDurations,
            controlMessage.RemainingTimeSec,
            controlMessage.CycleDurationSec,
            controlMessage.LocalOffsetSec,
            controlMessage.CycleProgressSec,
            controlMessage.PriorityLevel,
            controlMessage.IsFailoverActive
        );

        await _log.PublishAuditAsync(
            "TrafficOperator",
            $"Override applied to light {light.LightName} on intersection {intersection.Name}: {mode}",
            "[TRAFFIC-OPERATOR]",
            new Dictionary<string, object>
            {
            ["IntersectionId"] = intersection.IntersectionId,
            ["LightId"] = light.LightId,
            ["Mode"] = mode
            },
            "manual-override");

        _logger.LogInformation("[TRAFFIC-OPERATOR] Override applied to light {Light} at intersection {Intersection}: {Mode}", light.LightName, intersection.Name, mode);
    }
}
