using TrafficAnalyticsData.Entities;

namespace TrafficAnalyticsData.Repositories.Summary;


public interface IDailySummaryRepository
{
    Task<IEnumerable<DailySummaryEntity>> GetByDateAsync(DateTime date);
    Task<IEnumerable<DailySummaryEntity>> GetByIntersectionAsync(int intersectionId);
    Task InsertAsync(DailySummaryEntity entity);
    Task<IEnumerable<DailySummaryEntity>> GetDateRangeAsync(DateTime start, DateTime end, int intersectionId);
}
