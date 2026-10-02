using System;

namespace SensorStore.Models.Responses;

public class VehicleCountResponse
{
    public string VehicleId { get; set; } = string.Empty;
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int CountTotal { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double AverageWaitTimeSec { get; set; }
    public Dictionary<string, int>? CountByDirection { get; set; }
    public Dictionary<string, int>? VehicleBreakdown { get; set; }
}