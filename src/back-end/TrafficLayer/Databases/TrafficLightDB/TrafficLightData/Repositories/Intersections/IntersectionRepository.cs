using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrafficLightData.Entities;


namespace TrafficLightData.Repositories.Intersections;

public class IntersectionRepository : BaseRepository<IntersectionEntity>, IIntersectionRepository
{
    private readonly ILogger<IntersectionRepository> _logger;
    private const string domain = "[REPOSITORY][INTERSECTION]";

    public IntersectionRepository(TrafficLightDbContext context, ILogger<IntersectionRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<IntersectionEntity>> GetAllActiveAsync()
    {
        _logger.LogInformation("{Domain} Retrieving all active intersections\n", domain);
        return await _context.Intersections
            .Include(i => i.TrafficLights)
            .Include(i => i.Configurations)
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<IntersectionEntity?> GetByNameAsync(string name)
    {
        _logger.LogInformation("{Domain} Retrieving intersection by name: {Name}\n", domain, name);
        return await _context.Intersections
            .Include(i => i.TrafficLights)
            .Include(i => i.Configurations)
            .FirstOrDefaultAsync(i => i.Name == name);
    }
}