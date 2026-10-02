using System;

namespace DetectionStore.Models.Responses;

public class EmergencyVehicleDetectionResponse
{
    public string EmergencyId { get; set; } = string.Empty;
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string EmergencyVehicleType { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}