using System;

namespace Messages.Sensor.Detection;

public class PublicTransportDetectedMessage
{
    public string? CorrelationId { get; set; } 
    public int IntersectionId { get; set; }
    public string IntersectionName { get; set; } = default!;
    public string? LineName { get; set; }
    public DateTime DetectedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
