using System;

namespace TrafficLightCoordinatorStore.Publishers.Schedule;

public interface ITrafficLightSchedulePublisher
{
    Task PublishUpdateAsync(
        int intersectionId,
        string intersectionName,
        bool isOperational,
        string currentMode,
        Dictionary<string, int> phaseDurations,
        int cycleDurationSec,
        int globalOffsetSec,
        string? purpose = null);
}
