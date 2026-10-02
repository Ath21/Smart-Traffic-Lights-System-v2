using System;
using DetectionData.Collections.Count;

namespace DetectionData.Repositories.Cyclist;

public interface ICyclistCountRepository
{
    Task<IEnumerable<CyclistCountCollection>> GetRecentByIntersectionAsync(int intersectionId, int limit = 100);
    Task InsertAsync(CyclistCountCollection entity);
}