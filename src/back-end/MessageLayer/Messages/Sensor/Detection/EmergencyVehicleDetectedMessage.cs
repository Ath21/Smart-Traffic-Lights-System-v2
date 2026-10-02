using System;

namespace Messages.Sensor.Detection;

public class EmergencyVehicleDetectedMessage
{
    public string? CorrelationId { get; set; }
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = default!;
    public DateTime DetectedAt { get; set; }
    public string? Direction { get; set; }
    public string? EmergencyVehicleType { get; set; } // e.g. Ambulance, Firetruck
    public Dictionary<string, object>? Metadata { get; set; }
}
