using TrafficAnalyticsData.Entities;

namespace TrafficAnalyticsData.Repositories.Alerts;


public interface IAlertRepository
{
    Task<IEnumerable<AlertEntity>> GetRecentAlertsAsync(int limit = 20);
    Task<IEnumerable<AlertEntity>> GetByIntersectionAsync(int intersectionId);
    Task InsertAsync(AlertEntity entity);
    Task DeleteOldAlertsAsync(DateTime beforeDate);
}
