using System;

namespace TrafficLightCacheData.Repositories;

public interface ITrafficLightCacheRepository
{
    // CORE STATE
    Task SetStateAsync(int intersectionId, int lightId, string state);
    Task<string?> GetStateAsync(int intersectionId, int lightId);
    Task SetDurationAsync(int intersectionId, int lightId, int seconds);
    Task<int> GetDurationAsync(int intersectionId, int lightId);
    Task SetLastUpdateAsync(int intersectionId, int lightId, DateTime timestamp);
    Task<DateTime?> GetLastUpdateAsync(int intersectionId, int lightId);

    // PHASE STATE (NEW)
    Task SetCurrentPhaseAsync(int intersectionId, int lightId, string phase);
    Task<string?> GetCurrentPhaseAsync(int intersectionId, int lightId);
    Task SetRemainingTimeAsync(int intersectionId, int lightId, int seconds);
    Task<int> GetRemainingTimeAsync(int intersectionId, int lightId);

    // SYNCHRONIZATION
    Task SetCycleDurationAsync(int intersectionId, int lightId, int seconds);
    Task<int> GetCycleDurationAsync(int intersectionId, int lightId);
    Task SetOffsetAsync(int intersectionId, int lightId, int seconds);
    Task<int> GetOffsetAsync(int intersectionId, int lightId);
    Task SetLocalOffsetAsync(int intersectionId, int lightId, int seconds);
    Task<int> GetLocalOffsetAsync(int intersectionId, int lightId);
    Task SetCycleProgressAsync(int intersectionId, int lightId, double progressSec);
    Task<double> GetCycleProgressAsync(int intersectionId, int lightId);

    // CONFIGURATION & PRIORITY
    Task SetModeAsync(int intersectionId, int lightId, string mode);
    Task<string?> GetModeAsync(int intersectionId, int lightId);
    Task SetPriorityAsync(int intersectionId, int lightId, int priority);
    Task<int> GetPriorityAsync(int intersectionId, int lightId);
    Task SetCachedPhasesAsync(int intersectionId, int lightId, Dictionary<string, int> phases);
    Task<Dictionary<string, int>?> GetCachedPhasesAsync(int intersectionId, int lightId);

    // FAILOVER
    Task SetFailoverAsync(int intersectionId, int lightId, bool active);
    Task<bool> GetFailoverAsync(int intersectionId, int lightId);

    // DIAGNOSTICS
    Task SetHeartbeatAsync(int intersectionId, int lightId);
    Task<DateTime?> GetHeartbeatAsync(int intersectionId, int lightId);
    Task SetCoordinatorSyncAsync(int intersectionId);
}
