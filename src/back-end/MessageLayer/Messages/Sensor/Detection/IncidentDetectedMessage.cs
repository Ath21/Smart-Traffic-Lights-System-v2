using System;

namespace Messages.Sensor.Detection;

public class IncidentDetectedMessage
{
    public string? CorrelationId { get; set; }
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = default!;
    public DateTime ReportedAt { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
