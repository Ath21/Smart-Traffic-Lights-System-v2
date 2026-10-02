using System;

namespace DetectionStore.Models.Requests;

public class PublicTransportDetectionRequest
{
    public int IntersectionId { get; set; }
    public string IntersectionName { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;  // “Bus829”
    public string Line { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}
