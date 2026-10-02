using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TrafficLightCacheData.Keys;

namespace TrafficLightCacheData.Repositories;

public class TrafficLightCacheRepository : ITrafficLightCacheRepository
{
    private readonly TrafficLightCacheDbContext _context;
    private readonly ILogger<TrafficLightCacheRepository> _logger;
    private const string domain = "[REPOSITORY][TRAFFIC_LIGHT_CACHE]";

    public TrafficLightCacheRepository(TrafficLightCacheDbContext context, ILogger<TrafficLightCacheRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ===============================================================
    // CORE STATE
    // ===============================================================

    public async Task SetStateAsync(int intersectionId, int lightId, string state)
    {
        _logger.LogInformation("{Domain} Setting state for intersection {IntersectionId}, light {LightId} to {State}\n", domain, intersectionId, lightId, state);
        await _context.SetAsync(TrafficLightCacheKeys.State(intersectionId, lightId), state);
    }

    public async Task<string?> GetStateAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting state for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        return await _context.GetAsync(TrafficLightCacheKeys.State(intersectionId, lightId));
    }

    public async Task SetDurationAsync(int intersectionId, int lightId, int seconds)
    {
        _logger.LogInformation("{Domain} Setting duration for intersection {IntersectionId}, light {LightId} to {Seconds}\n", domain, intersectionId, lightId, seconds);
        await _context.SetAsync(TrafficLightCacheKeys.Duration(intersectionId, lightId), seconds.ToString());
    }

    public async Task<int> GetDurationAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting duration for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.Duration(intersectionId, lightId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    public async Task SetLastUpdateAsync(int intersectionId, int lightId, DateTime timestamp)
    {
        _logger.LogInformation("{Domain} Setting last update for intersection {IntersectionId}, light {LightId} to {Timestamp}\n", domain, intersectionId, lightId, timestamp);
        await _context.SetAsync(TrafficLightCacheKeys.LastUpdate(intersectionId, lightId), timestamp.ToString("o"));
    }

    public async Task<DateTime?> GetLastUpdateAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting last update for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.LastUpdate(intersectionId, lightId));
        return DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
    }

    // ===============================================================
    // PHASE STATE (NEW)
    // ===============================================================

    public async Task SetCurrentPhaseAsync(int intersectionId, int lightId, string phase)
    {
        _logger.LogInformation("{Domain} Setting current phase for intersection {IntersectionId}, light {LightId} to {Phase}\n", domain, intersectionId, lightId, phase);
        await _context.SetAsync(TrafficLightCacheKeys.CurrentPhase(intersectionId, lightId), phase);
    }

    public async Task<string?> GetCurrentPhaseAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting current phase for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        return await _context.GetAsync(TrafficLightCacheKeys.CurrentPhase(intersectionId, lightId));
    }

    public async Task SetRemainingTimeAsync(int intersectionId, int lightId, int seconds)
    {
        _logger.LogInformation("{Domain} Setting remaining time for intersection {IntersectionId}, light {LightId} to {Seconds}\n", domain, intersectionId, lightId, seconds);
        await _context.SetAsync(TrafficLightCacheKeys.RemainingTime(intersectionId, lightId), seconds.ToString());
    }

    public async Task<int> GetRemainingTimeAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting remaining time for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.RemainingTime(intersectionId, lightId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    // ===============================================================
    // SYNCHRONIZATION
    // ===============================================================

    public async Task SetCycleDurationAsync(int intersectionId, int lightId, int seconds)
    {
        _logger.LogInformation("{Domain} Setting cycle duration for intersection {IntersectionId}, light {LightId} to {Seconds}\n", domain, intersectionId, lightId, seconds);
        await _context.SetAsync(TrafficLightCacheKeys.CycleDuration(intersectionId, lightId), seconds.ToString());
    }

    public async Task<int> GetCycleDurationAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting cycle duration for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.CycleDuration(intersectionId, lightId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    public async Task SetOffsetAsync(int intersectionId, int lightId, int seconds)
    {
        _logger.LogInformation("{Domain} Setting offset for intersection {IntersectionId}, light {LightId} to {Seconds}\n", domain, intersectionId, lightId, seconds);
        await _context.SetAsync(TrafficLightCacheKeys.Offset(intersectionId, lightId), seconds.ToString());
    }

    public async Task<int> GetOffsetAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting offset for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.Offset(intersectionId, lightId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    // NEW: local offset & cycle progress
    public async Task SetLocalOffsetAsync(int intersectionId, int lightId, int seconds)
    {
        _logger.LogInformation("{Domain} Setting local offset for intersection {IntersectionId}, light {LightId} to {Seconds}\n", domain, intersectionId, lightId, seconds);
        await _context.SetAsync(TrafficLightCacheKeys.LocalOffset(intersectionId, lightId), seconds.ToString());
    }

    public async Task<int> GetLocalOffsetAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting local offset for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.LocalOffset(intersectionId, lightId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    public async Task SetCycleProgressAsync(int intersectionId, int lightId, double progressSec)
    {
        _logger.LogInformation("{Domain} Setting cycle progress for intersection {IntersectionId}, light {LightId} to {ProgressSec}\n", domain, intersectionId, lightId, progressSec);
        await _context.SetAsync(TrafficLightCacheKeys.CycleProgress(intersectionId, lightId), progressSec.ToString(CultureInfo.InvariantCulture));
    }

    public async Task<double> GetCycleProgressAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting cycle progress for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.CycleProgress(intersectionId, lightId));
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
    }

    // ===============================================================
    // CONFIGURATION & PRIORITY
    // ===============================================================

    public async Task SetModeAsync(int intersectionId, int lightId, string mode)
    {
        _logger.LogInformation("{Domain} Setting mode for intersection {IntersectionId}, light {LightId} to {Mode}\n", domain, intersectionId, lightId, mode);
        await _context.SetAsync(TrafficLightCacheKeys.Mode(intersectionId, lightId), mode);
    }

    public async Task<string?> GetModeAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting mode for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        return await _context.GetAsync(TrafficLightCacheKeys.Mode(intersectionId, lightId));
    }

    public async Task SetPriorityAsync(int intersectionId, int lightId, int priority)
    {
        _logger.LogInformation("{Domain} Setting priority for intersection {IntersectionId}, light {LightId} to {Priority}\n", domain, intersectionId, lightId, priority);
        await _context.SetAsync(TrafficLightCacheKeys.Priority(intersectionId, lightId), priority.ToString());
    }

    public async Task<int> GetPriorityAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting priority for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.Priority(intersectionId, lightId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    // Cache serialized phase configuration
    public async Task SetCachedPhasesAsync(int intersectionId, int lightId, Dictionary<string, int> phases)
    {

        _logger.LogInformation("{Domain} Caching phases for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId); 
        await _context.SetAsync(
            TrafficLightCacheKeys.CachedPhases(intersectionId, lightId),
            JsonSerializer.Serialize(phases));

    }

    public async Task<Dictionary<string, int>?> GetCachedPhasesAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Retrieving cached phases for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var json = await _context.GetAsync(TrafficLightCacheKeys.CachedPhases(intersectionId, lightId));
        return json is null ? null : JsonSerializer.Deserialize<Dictionary<string, int>>(json);
    }

    // ===============================================================
    // FAILOVER
    // ===============================================================

    public async Task SetFailoverAsync(int intersectionId, int lightId, bool active)
    {
        _logger.LogInformation("{Domain} Setting failover for intersection {IntersectionId}, light {LightId} to {Active}\n", domain, intersectionId, lightId, active);
        await _context.SetAsync(TrafficLightCacheKeys.FailoverActive(intersectionId, lightId), active.ToString());
    }

    public async Task<bool> GetFailoverAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting failover for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.FailoverActive(intersectionId, lightId));
        return bool.TryParse(value, out var result) && result;
    }

    // ===============================================================
    // DIAGNOSTICS
    // ===============================================================

    public async Task SetHeartbeatAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Setting heartbeat for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        await _context.SetAsync(
            TrafficLightCacheKeys.Heartbeat(intersectionId, lightId),
            DateTime.UtcNow.ToString("o"),
            TimeSpan.FromSeconds(10));
    }

    public async Task<DateTime?> GetHeartbeatAsync(int intersectionId, int lightId)
    {
        _logger.LogInformation("{Domain} Getting heartbeat for intersection {IntersectionId}, light {LightId}\n", domain, intersectionId, lightId);
        var value = await _context.GetAsync(TrafficLightCacheKeys.Heartbeat(intersectionId, lightId));
        return DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
    }

    public async Task SetCoordinatorSyncAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Setting coordinator sync for intersection {IntersectionId}\n", domain, intersectionId);
        await _context.SetAsync(
            TrafficLightCacheKeys.LastCoordinatorSync(intersectionId),
            DateTime.UtcNow.ToString("o"));
    }
}
