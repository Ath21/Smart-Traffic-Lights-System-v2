using System;
using TrafficAnalyticsStore.Models;

namespace TrafficAnalyticsStore.Business.Alerts;

public interface IAlertBusiness
{
    Task<IEnumerable<AlertDto>> GetAlertsAsync(
        string? type = null,
        string? intersection = null,
        DateTime? from = null,
        DateTime? to = null);

    Task<AlertDto> CreateAlertAsync(
        int intersectionId,
        string intersection,
        string type,
        string message,
        double congestionIndex,
        int severity);
}
