using System;

namespace DetectionStore.Models.Requests;

public class EmergencyVehicleDetectionRequest
{
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string EmergencyVehicleType { get; set; } = string.Empty; // ambulance, firetruck, police
    public DateTime DetectedAt { get; set; }
}