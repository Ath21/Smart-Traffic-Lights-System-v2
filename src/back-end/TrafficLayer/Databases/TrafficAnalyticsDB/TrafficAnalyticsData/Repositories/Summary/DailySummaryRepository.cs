using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrafficAnalyticsData;
using TrafficAnalyticsData.Entities;

namespace TrafficAnalyticsData.Repositories.Summary;

public class DailySummaryRepository : BaseRepository<DailySummaryEntity>, IDailySummaryRepository
{
    private readonly ILogger<DailySummaryRepository> _logger;
    private const string domain = "[REPOSITORY][DAILY_SUMMARY]";

    public DailySummaryRepository(TrafficAnalyticsDbContext context, ILogger<DailySummaryRepository> logger)
        : base(context) 
    { 
        _logger = logger;
    }

    public async Task<IEnumerable<DailySummaryEntity>> GetByDateAsync(DateTime date)
    {
        _logger.LogInformation("{Domain} Getting daily summary for date {Date}\n", domain, date);
        return await _context.DailySummaries
            .Where(s => s.Date == date.Date)
            .OrderBy(s => s.Intersection)
            .ToListAsync();
    }

    public async Task<IEnumerable<DailySummaryEntity>> GetByIntersectionAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Getting daily summary for intersection {IntersectionId}\n", domain, intersectionId);
        return await _context.DailySummaries
            .Where(s => s.IntersectionId == intersectionId)
            .OrderByDescending(s => s.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<DailySummaryEntity>> GetDateRangeAsync(DateTime start, DateTime end, int intersectionId)
    {
        _logger.LogInformation("{Domain} Getting daily summary for intersection {IntersectionId} between {Start} and {End}\n", domain, intersectionId, start, end);
        return await _context.DailySummaries
            .Where(s => s.IntersectionId == intersectionId && s.Date >= start.Date && s.Date <= end.Date)
            .OrderBy(s => s.Date)
            .ToListAsync();
    }
}
