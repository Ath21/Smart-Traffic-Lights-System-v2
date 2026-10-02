using System;
using DetectionData;
using DetectionData.Collections.Detection;
using MongoDB.Driver;

namespace DetectionData.Repositories.EmergencyVehicle;


public class EmergencyVehicleDetectionRepository : BaseRepository<EmergencyVehicleDetectionCollection>, IEmergencyVehicleDetectionRepository
{
    public EmergencyVehicleDetectionRepository(DetectionDbContext context)
        : base(context.EmergencyVehicleDetections) { }

    public async Task<IEnumerable<EmergencyVehicleDetectionCollection>> GetRecentEmergenciesAsync(int intersectionId, int limit = 50)
    {
        var filter = Builders<EmergencyVehicleDetectionCollection>.Filter.Eq(x => x.IntersectionId, intersectionId);
        return await _collection.Find(filter)
            .SortByDescending(x => x.DetectedAt)
            .Limit(limit)
            .ToListAsync();
    }
}