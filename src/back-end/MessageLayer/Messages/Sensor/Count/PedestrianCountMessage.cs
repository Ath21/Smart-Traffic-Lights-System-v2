using System;

namespace Messages.Sensor.Count;

public class PedestrianCountMessage
{
    public string? CorrelationId { get; set; }
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}
