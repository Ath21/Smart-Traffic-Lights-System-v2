using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrafficLightData.Entities;


namespace TrafficLightData.Repositories.Light;

public class TrafficLightRepository : BaseRepository<TrafficLightEntity>, ITrafficLightRepository
{
    private readonly ILogger<TrafficLightRepository> _logger;
    private const string domain = "[REPOSITORY][TRAFFIC_LIGHT]";

    public TrafficLightRepository(TrafficLightDbContext context, ILogger<TrafficLightRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TrafficLightEntity>> GetByIntersectionAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving traffic lights for intersection {IntersectionId}\n", domain, intersectionId);
        return await _context.TrafficLights
            .Where(l => l.IntersectionId == intersectionId)
            .OrderBy(l => l.LightName)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int lightId, bool isOperational)
    {
        _logger.LogInformation("{Domain} Updating status for traffic light {LightId} to {IsOperational}\n", domain, lightId, isOperational);
        var light = await _context.TrafficLights.FindAsync(lightId);
        if (light != null)
        {
            light.IsOperational = isOperational;
            await _context.SaveChangesAsync();
        }
    }
}