using System;
using DetectionData;
using DetectionData.Collections.Count;
using MongoDB.Driver;

namespace DetectionData.Repositories.Pedestrian;

public class PedestrianCountRepository : BaseRepository<PedestrianCountCollection>, IPedestrianCountRepository
{
    public PedestrianCountRepository(DetectionDbContext context)
        : base(context.PedestrianCount) { }

    public async Task<IEnumerable<PedestrianCountCollection>> GetRecentByIntersectionAsync(int intersectionId, int limit = 100)
    {
        var filter = Builders<PedestrianCountCollection>.Filter.Eq(x => x.IntersectionId, intersectionId);
        var result = await _collection.Find(filter)
                                      .SortByDescending(x => x.Timestamp)
                                      .Limit(limit)
                                      .ToListAsync();
        return result;
    }
}