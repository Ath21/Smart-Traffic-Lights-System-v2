using System;
using DetectionData;
using DetectionData.Collections.Count;
using MongoDB.Driver;

namespace DetectionData.Repositories.Vehicle;


public class VehicleCountRepository : BaseRepository<VehicleCountCollection>, IVehicleCountRepository
{
    public VehicleCountRepository(DetectionDbContext context)
        : base(context.VehicleCount) { }

    public async Task<IEnumerable<VehicleCountCollection>> GetRecentByIntersectionAsync(int intersectionId, int limit = 100)
    {
        var filter = Builders<VehicleCountCollection>.Filter.Eq(x => x.IntersectionId, intersectionId);
        var result = await _collection.Find(filter)
                                      .SortByDescending(x => x.Timestamp)
                                      .Limit(limit)
                                      .ToListAsync();
        return result;
    }
}