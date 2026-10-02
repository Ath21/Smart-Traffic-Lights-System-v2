using System.Threading.Tasks;

namespace DetectionCacheData.Repositories;

public interface IDetectionCacheRepository
{
    // Vehicle Count
    Task SetVehicleCountAsync(int intersectionId, int count);
    Task<int> GetVehicleCountAsync(int intersectionId);

    // Pedestrian Count
    Task SetPedestrianCountAsync(int intersectionId, int count);
    Task<int> GetPedestrianCountAsync(int intersectionId);

    // Cyclist Count
    Task SetCyclistCountAsync(int intersectionId, int count);
    Task<int> GetCyclistCountAsync(int intersectionId);

    // Emergency / Incident Flags
    Task SetEmergencyDetectedAsync(int intersectionId, bool detected);
    Task<bool> GetEmergencyDetectedAsync(int intersectionId);

    Task SetIncidentDetectedAsync(int intersectionId, bool detected);
    Task<bool> GetIncidentDetectedAsync(int intersectionId);

    Task SetPublicTransportDetectedAsync(int intersectionId, bool detected);
    Task<bool> GetPublicTransportDetectedAsync(int intersectionId);
}
