using System;
using DetectionData.Collections.Count;

namespace DetectionData.Repositories.Pedestrian;

public interface IPedestrianCountRepository
{
    Task<IEnumerable<PedestrianCountCollection>> GetRecentByIntersectionAsync(int intersectionId, int limit = 100);
    Task InsertAsync(PedestrianCountCollection entity);
}
