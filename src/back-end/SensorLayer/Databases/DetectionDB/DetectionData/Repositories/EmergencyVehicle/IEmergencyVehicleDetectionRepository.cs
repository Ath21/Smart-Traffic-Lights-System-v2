using System;
using DetectionData.Collections.Detection;

namespace DetectionData.Repositories.EmergencyVehicle;

public interface IEmergencyVehicleDetectionRepository
{
    Task<IEnumerable<EmergencyVehicleDetectionCollection>> GetRecentEmergenciesAsync(int intersectionId, int limit = 50);
    Task InsertAsync(EmergencyVehicleDetectionCollection entity);
}