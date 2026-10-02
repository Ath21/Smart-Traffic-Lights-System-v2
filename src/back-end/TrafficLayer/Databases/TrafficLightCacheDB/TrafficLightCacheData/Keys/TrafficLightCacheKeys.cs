namespace TrafficLightCacheData.Keys;

public static class TrafficLightCacheKeys
{
    // Current active phase (Green, Yellow, Red)
    public static string CurrentPhase(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:phase";

    // Remaining seconds for the current phase
    public static string RemainingTime(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:remaining";

    // Local offset (relative to intersection cycle)
    public static string LocalOffset(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:local_offset";

    // Everything else stays as before:
    public static string State(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:state";

    public static string Duration(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:duration";

    public static string LastUpdate(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:last_update";

    public static string CycleDuration(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:cycle_duration";

    public static string Offset(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:offset";

    public static string CycleProgress(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:cycle_progress";

    public static string Mode(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:mode";

    public static string Priority(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:priority";

    public static string CachedPhases(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:cached_phases";

    public static string FailoverActive(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:failover";

    public static string LastCoordinatorSync(int intersectionId)
        => $"traffic_light:{intersectionId}:last_sync";

    public static string Heartbeat(int intersectionId, int lightId)
        => $"traffic_light:{intersectionId}:{lightId}:heartbeat";
}
