using DetectionCacheData.Keys;
using Microsoft.Extensions.Logging;

namespace DetectionCacheData.Repositories;

public class DetectionCacheRepository : IDetectionCacheRepository
{
    private readonly DetectionCacheDbContext _context;
    private const string domain = "[REPOSITORY][DETECTION_CACHE]";
    private readonly ILogger<DetectionCacheRepository> _logger;

    public DetectionCacheRepository(DetectionCacheDbContext context, ILogger<DetectionCacheRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ===============================
    // Vehicle Count
    // ===============================
    public async Task SetVehicleCountAsync(int intersectionId, int count)
    {
        _logger.LogInformation("{Domain} Setting vehicle count for intersection {IntersectionId} to {Count}\n", domain, intersectionId, count);
        await _context.SetValueAsync(DetectionCacheKeys.VehicleCount(intersectionId), count.ToString());
    }

    public async Task<int> GetVehicleCountAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving vehicle count for intersection {IntersectionId}\n", domain, intersectionId);
        var value = await _context.GetValueAsync(DetectionCacheKeys.VehicleCount(intersectionId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    // ===============================
    // Pedestrian Count
    // ===============================
    public async Task SetPedestrianCountAsync(int intersectionId, int count)
    {
        _logger.LogInformation("{Domain} Setting pedestrian count for intersection {IntersectionId} to {Count}\n", domain, intersectionId, count);
        await _context.SetValueAsync(DetectionCacheKeys.PedestrianCount(intersectionId), count.ToString());
    }

    public async Task<int> GetPedestrianCountAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving pedestrian count for intersection {IntersectionId}\n", domain, intersectionId);
        var value = await _context.GetValueAsync(DetectionCacheKeys.PedestrianCount(intersectionId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    // ===============================
    // Cyclist Count
    // ===============================
    public async Task SetCyclistCountAsync(int intersectionId, int count)
    {
        _logger.LogInformation("{Domain} Setting cyclist count for intersection {IntersectionId} to {Count}\n", domain, intersectionId, count);
        await _context.SetValueAsync(DetectionCacheKeys.CyclistCount(intersectionId), count.ToString());
    }

    public async Task<int> GetCyclistCountAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving cyclist count for intersection {IntersectionId}\n", domain, intersectionId);
        var value = await _context.GetValueAsync(DetectionCacheKeys.CyclistCount(intersectionId));
        return int.TryParse(value, out var result) ? result : 0;
    }

    // ===============================
    // Emergency Flags
    // ===============================
    public async Task SetEmergencyDetectedAsync(int intersectionId, bool detected)
    {
        _logger.LogInformation("{Domain} Setting emergency detected for intersection {IntersectionId} to {Detected}\n", domain, intersectionId, detected);
        await _context.SetValueAsync(DetectionCacheKeys.EmergencyDetected(intersectionId), detected.ToString());
    }

    public async Task<bool> GetEmergencyDetectedAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving emergency detected for intersection {IntersectionId}\n", domain, intersectionId);
        var value = await _context.GetValueAsync(DetectionCacheKeys.EmergencyDetected(intersectionId));
        return bool.TryParse(value, out var result) && result;
    }

    // ===============================
    // Incident Flags
    // ===============================
    public async Task SetIncidentDetectedAsync(int intersectionId, bool detected)
    {
        _logger.LogInformation("{Domain} Setting incident detected for intersection {IntersectionId} to {Detected}\n", domain, intersectionId, detected);
        await _context.SetValueAsync(DetectionCacheKeys.IncidentDetected(intersectionId), detected.ToString());
    }

    public async Task<bool> GetIncidentDetectedAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving incident detected for intersection {IntersectionId}\n", domain, intersectionId);
        var value = await _context.GetValueAsync(DetectionCacheKeys.IncidentDetected(intersectionId));
        return bool.TryParse(value, out var result) && result;
    }

    // ===============================
    // Public Transport Flags
    // ===============================
    public async Task SetPublicTransportDetectedAsync(int intersectionId, bool detected)
    {
        _logger.LogInformation("{Domain} Setting public transport detected for intersection {IntersectionId} to {Detected}\n", domain, intersectionId, detected);
        await _context.SetValueAsync(DetectionCacheKeys.PublicTransportDetected(intersectionId), detected.ToString());
    }

    public async Task<bool> GetPublicTransportDetectedAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving public transport detected for intersection {IntersectionId}\n", domain, intersectionId);
        var value = await _context.GetValueAsync(DetectionCacheKeys.PublicTransportDetected(intersectionId));
        return bool.TryParse(value, out var result) && result;
    }
}
