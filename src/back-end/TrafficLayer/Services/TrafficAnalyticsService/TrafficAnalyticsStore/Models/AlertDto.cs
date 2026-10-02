using System;

namespace TrafficAnalyticsStore.Models;

public class AlertDto
{
    public int AlertId { get; set; }

    public int IntersectionId { get; set; }

    public string Intersection { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty; // e.g. "Incident", "Congestion"

    public string Message { get; set; } = string.Empty;

    public int Severity { get; set; }

    public double CongestionIndex { get; set; }

    public DateTime CreatedAt { get; set; }
}
