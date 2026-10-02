using System;

namespace Messages.Traffic.Analytics;

public class SummaryAnalyticsMessage
{
    public string Intersection { get; set; } = null!;
    public int VehicleCount { get; set; }
    public int PedestrianCount { get; set; }
    public int CyclistCount { get; set; }
    public int IncidentsDetected { get; set; }
    public double AverageCongestion { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}