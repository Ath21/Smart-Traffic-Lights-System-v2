using System;

namespace TrafficAnalyticsStore.Models;

public class DailySummaryDto
{
    public int SummaryId { get; set; }

    public int IntersectionId { get; set; }

    public string Intersection { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int TotalVehicles { get; set; }

    public int TotalPedestrians { get; set; }

    public int TotalCyclists { get; set; }

    public double AverageSpeedKmh { get; set; }

    public double AverageWaitTimeSec { get; set; }

    public double CongestionIndex { get; set; }
}