using System;

namespace Messages.Sensor.Count;

public class VehicleCountMessage
{
    public string? CorrelationId { get; set; }
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public int CountTotal { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double AverageWaitTimeSec { get; set; }
    public double? FlowRate { get; set; }
    public Dictionary<string, int>? VehicleBreakdown { get; set; } // car, truck, etc.
}
