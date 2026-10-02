using System;
using DetectionData.Collections.Count;

namespace DetectionData.Repositories.Vehicle;

public interface IVehicleCountRepository
{
    Task<IEnumerable<VehicleCountCollection>> GetRecentByIntersectionAsync(int intersectionId, int limit = 100);
    Task InsertAsync(VehicleCountCollection entity);
}
