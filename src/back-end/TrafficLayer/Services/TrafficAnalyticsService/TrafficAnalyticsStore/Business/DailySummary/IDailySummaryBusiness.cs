using System;
using TrafficAnalyticsStore.Models;

namespace TrafficAnalyticsStore.Business.DailySummary;

public interface IDailySummaryBusiness
{
    Task<IEnumerable<DailySummaryDto>> GetSummariesAsync(
        int? intersectionId = null,
        string? intersection = null,
        DateTime? from = null,
        DateTime? to = null);

    Task<DailySummaryDto> GetOrCreateTodayAsync(int intersectionId, string intersection);
    Task UpdateCountsAsync(DailySummaryDto summary, string countType, int count, double avgSpeed, double avgWait);
    Task SaveAsync(DailySummaryDto summary);

    Task<byte[]> ExportSummariesCsvAsync(
        int? intersectionId = null,
        string? intersection = null,
        DateTime? from = null,
        DateTime? to = null);
}
