using System;

namespace Messages.Traffic.Analytics;

public class IncidentAnalyticsMessage
{
    public string Intersection { get; set; } = null!;
    public string IncidentType { get; set; } = null!;    // e.g., "collision", "obstruction"
    public int Severity { get; set; }   
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
