using System;

namespace TrafficLightCoordinatorStore.Publishers.Control;

public interface ITrafficLightControlPublisher
{
    Task PublishControlAsync(
        int intersectionId,
        string intersectionName,
        int lightId,
        string lightName,
        string operationalMode,
        Dictionary<string, int> phaseDurations,
        int remainingTimeSec,
        int cycleDurationSec,
        int localOffsetSec,
        double cycleProgressSec,
        int priorityLevel,
        bool isFailoverActive,
        string? timePlan = null);
}
