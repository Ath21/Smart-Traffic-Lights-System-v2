using System;

namespace Messages.Traffic.Analytics;

public class CongestionAnalyticsMessage
{
    public string Intersection { get; set; } = null!;
    public double CongestionLevel { get; set; }           // 0–1 normalized or %
    public string Status { get; set; } = "normal";        // normal | moderate | heavy
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
