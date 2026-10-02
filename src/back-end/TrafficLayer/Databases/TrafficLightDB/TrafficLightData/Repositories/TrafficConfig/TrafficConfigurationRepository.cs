using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrafficLightData.Entities;


namespace TrafficLightData.Repositories.TrafficConfig;

public class TrafficConfigurationRepository : BaseRepository<TrafficConfigurationEntity>, ITrafficConfigurationRepository
{
    private readonly ILogger<TrafficConfigurationRepository> _logger;
    private const string domain = "[REPOSITORY][TRAFFIC_CONFIG]";

    public TrafficConfigurationRepository(TrafficLightDbContext context, ILogger<TrafficConfigurationRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<TrafficConfigurationEntity?> GetLatestByIntersectionAsync(int configId)
    {
        _logger.LogInformation("{domain} Fetching latest configuration for ConfigID: {configId}\n", domain, configId);
        return await _context.TrafficConfigurations
            .Where(c => c.ConfigurationId == configId)
            .OrderByDescending(c => c.LastUpdated)
            .FirstOrDefaultAsync();
    }
    public async Task<IEnumerable<TrafficConfigurationEntity>> GetByModeAsync(string mode)
    {
        _logger.LogInformation("{domain} Fetching configurations for Mode: {mode}\n", domain, mode);
        return await _context.TrafficConfigurations
            .Where(c => c.Mode == mode)
            .OrderByDescending(c => c.LastUpdated)
            .ToListAsync();
    }

}
