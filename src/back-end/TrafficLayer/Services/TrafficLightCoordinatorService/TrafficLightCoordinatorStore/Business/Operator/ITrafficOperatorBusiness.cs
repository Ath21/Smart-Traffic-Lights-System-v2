using System;

namespace TrafficLightCoordinatorStore.Business.Operator;

public interface ITrafficOperatorBusiness
{
    Task ApplyModeAsync(int intersectionId, string mode);

    Task OverrideLightAsync(
        int intersectionId,
        int lightId,
        string mode,
        Dictionary<string, int>? phaseDurations = null,
        int remainingTimeSec = 0,
        int cycleDurationSec = 60,
        int localOffsetSec = 0,
        double cycleProgressSec = 0,
        int priorityLevel = 1,
        bool isFailoverActive = false);
}

