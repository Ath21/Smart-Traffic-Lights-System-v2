using System;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TrafficAnalyticsData;
using TrafficAnalyticsData.Entities;
using TrafficAnalyticsStore.Models;

namespace TrafficAnalyticsStore.Business.DailySummary;

public class DailySummaryBusiness : IDailySummaryBusiness
{
    private readonly TrafficAnalyticsDbContext _db;
    private readonly ILogger<DailySummaryBusiness> _logger;

    public DailySummaryBusiness(TrafficAnalyticsDbContext db, ILogger<DailySummaryBusiness> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<DailySummaryDto>> GetSummariesAsync(
        int? intersectionId = null,
        string? intersection = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = _db.DailySummaries.AsQueryable();

        if (intersectionId.HasValue)
            query = query.Where(s => s.IntersectionId == intersectionId.Value);

        if (!string.IsNullOrWhiteSpace(intersection))
            query = query.Where(s => s.Intersection!.ToLower() == intersection.ToLower());

        if (from.HasValue)
            query = query.Where(s => s.Date >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(s => s.Date <= to.Value.Date);

        var list = await query.OrderBy(s => s.Date).ToListAsync();

        return list.Select(MapToModel).ToList();
    }

    public async Task<DailySummaryDto> GetOrCreateTodayAsync(int intersectionId, string intersection)
    {
        var entity = await _db.DailySummaries
            .FirstOrDefaultAsync(s => s.IntersectionId == intersectionId && s.Date.Date == DateTime.UtcNow.Date);

        if (entity == null)
        {
            entity = new DailySummaryEntity
            {
                IntersectionId = intersectionId,
                Intersection = intersection,
                Date = DateTime.UtcNow.Date
            };
            _db.DailySummaries.Add(entity);
            await _db.SaveChangesAsync();
        }

        return MapToModel(entity);
    }

    public async Task UpdateCountsAsync(DailySummaryDto model, string countType, int count, double avgSpeed, double avgWait)
    {
        var entity = await _db.DailySummaries.FirstAsync(x => x.SummaryId == model.SummaryId);

        switch (countType)
        {
            case "Vehicle": entity.TotalVehicles += count; break;
            case "Pedestrian": entity.TotalPedestrians += count; break;
            case "Cyclist": entity.TotalCyclists += count; break;
        }

        entity.AverageSpeedKmh = (entity.AverageSpeedKmh + avgSpeed) / 2.0;
        entity.AverageWaitTimeSec = (entity.AverageWaitTimeSec + avgWait) / 2.0;

        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync(DailySummaryDto model)
    {
        var entity = await _db.DailySummaries.FirstAsync(x => x.SummaryId == model.SummaryId);

        entity.AverageSpeedKmh = model.AverageSpeedKmh;
        entity.AverageWaitTimeSec = model.AverageWaitTimeSec;
        entity.TotalVehicles = model.TotalVehicles;
        entity.TotalPedestrians = model.TotalPedestrians;
        entity.TotalCyclists = model.TotalCyclists;

        await _db.SaveChangesAsync();
    }

    public async Task<byte[]> ExportSummariesCsvAsync(
        int? intersectionId = null,
        string? intersection = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var summaries = await GetSummariesAsync(intersectionId, intersection, from, to);

        if (!summaries.Any())
        {
            _logger.LogWarning("No data found to export for filters: {Intersection}", intersection ?? "all");
            return Array.Empty<byte>();
        }

        var sb = new StringBuilder();
        sb.AppendLine("Date,Intersection,TotalVehicles,TotalPedestrians,TotalCyclists,AverageSpeedKmh,AverageWaitTimeSec,CongestionIndex");

        foreach (var s in summaries)
        {
            sb.AppendLine(
                $"{s.Date:yyyy-MM-dd},{s.Intersection},{s.TotalVehicles},{s.TotalPedestrians},{s.TotalCyclists}," +
                $"{s.AverageSpeedKmh.ToString(CultureInfo.InvariantCulture)}," +
                $"{s.AverageWaitTimeSec.ToString(CultureInfo.InvariantCulture)}," +
                $"{s.CongestionIndex.ToString(CultureInfo.InvariantCulture)}");
        }

        _logger.LogInformation("Exported {Count} summaries to CSV for filters: {Intersection}", summaries.Count(), intersection ?? "all");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static DailySummaryDto MapToModel(DailySummaryEntity e)
    {
        return new DailySummaryDto
        {
            SummaryId = e.SummaryId,
            IntersectionId = e.IntersectionId,
            Intersection = e.Intersection!,
            Date = e.Date,
            TotalVehicles = e.TotalVehicles,
            TotalPedestrians = e.TotalPedestrians,
            TotalCyclists = e.TotalCyclists,
            AverageSpeedKmh = e.AverageSpeedKmh,
            AverageWaitTimeSec = e.AverageWaitTimeSec,
            CongestionIndex = ComputeCongestionIndex(e.AverageSpeedKmh, e.AverageWaitTimeSec, e.TotalVehicles)
        };
    }

    private static double ComputeCongestionIndex(double avgSpeed, double avgWait, int totalVehicles)
    {
        double speedFactor = avgSpeed <= 10 ? 1.0 : 1.0 - (avgSpeed / 100.0);
        double waitFactor = Math.Min(avgWait / 60.0, 1.0);
        double volumeFactor = Math.Min(totalVehicles / 2000.0, 1.0);
        return Math.Clamp((speedFactor + waitFactor + volumeFactor) / 3.0, 0.0, 1.0);
    }
}

