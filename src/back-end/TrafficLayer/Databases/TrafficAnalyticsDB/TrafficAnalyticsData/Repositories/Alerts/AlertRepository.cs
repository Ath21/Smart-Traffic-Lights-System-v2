using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrafficAnalyticsData;
using TrafficAnalyticsData.Entities;

namespace TrafficAnalyticsData.Repositories.Alerts;

public class AlertRepository : BaseRepository<AlertEntity>, IAlertRepository
{
    private readonly ILogger<AlertRepository> _logger;
    private const string domain = "[REPOSITORY][ALERT]";

    public AlertRepository(TrafficAnalyticsDbContext context, ILogger<AlertRepository> logger)
        : base(context) 
    {
        _logger = logger;
    }

    public async Task<IEnumerable<AlertEntity>> GetRecentAlertsAsync(int limit = 20)
    {
        _logger.LogInformation("{Domain} Getting recent alerts with limit {Limit}\n", domain, limit);
        return await _context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AlertEntity>> GetByIntersectionAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Getting alerts for intersection {IntersectionId}\n", domain, intersectionId);
        return await _context.Alerts
            .Where(a => a.IntersectionId == intersectionId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteOldAlertsAsync(DateTime beforeDate)
    {
        _logger.LogInformation("{Domain} Deleting alerts older than {BeforeDate}\n", domain, beforeDate);
        var oldAlerts = await _context.Alerts
            .Where(a => a.CreatedAt < beforeDate)
            .ToListAsync();

        if (oldAlerts.Any())
        {
            _context.Alerts.RemoveRange(oldAlerts);
            await _context.SaveChangesAsync();
        }
    }
}