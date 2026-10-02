using System;
using Microsoft.EntityFrameworkCore;
using TrafficAnalyticsData;
using TrafficAnalyticsData.Entities;
using TrafficAnalyticsStore.Models;

namespace TrafficAnalyticsStore.Business.Alerts;

public class AlertBusiness : IAlertBusiness
{
    private readonly TrafficAnalyticsDbContext _db;
    private readonly ILogger<AlertBusiness> _logger;

    public AlertBusiness(TrafficAnalyticsDbContext db, ILogger<AlertBusiness> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<AlertDto>> GetAlertsAsync(
        string? type = null,
        string? intersection = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = _db.Alerts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(a => a.Type!.ToLower() == type.ToLower());

        if (!string.IsNullOrWhiteSpace(intersection))
            query = query.Where(a => a.Intersection!.ToLower() == intersection.ToLower());

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var alerts = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        return alerts.Select(MapToModel).ToList();
    }

    public async Task<AlertDto> CreateAlertAsync(
        int intersectionId,
        string intersection,
        string type,
        string message,
        double congestionIndex,
        int severity)
    {
        var entity = new AlertEntity
        {
            IntersectionId = intersectionId,
            Intersection = intersection,
            Type = type,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        _db.Alerts.Add(entity);
        await _db.SaveChangesAsync();

        var model = MapToModel(entity);
        model.CongestionIndex = congestionIndex;
        model.Severity = severity;

        _logger.LogWarning("[ALERT][{Type}] {Intersection}: {Message}", type, intersection, message);
        return model;
    }

    private static AlertDto MapToModel(AlertEntity e)
    {
        return new AlertDto
        {
            AlertId = e.AlertId,
            IntersectionId = e.IntersectionId,
            Intersection = e.Intersection!,
            Type = e.Type!,
            Message = e.Message!,
            CreatedAt = e.CreatedAt,
            Severity = 0,
            CongestionIndex = 0
        };
    }
}
