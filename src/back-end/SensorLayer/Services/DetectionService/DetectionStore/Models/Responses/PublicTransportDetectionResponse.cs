using System;

namespace DetectionStore.Models.Responses;

public class PublicTransportDetectionResponse
{
    public string PublicId { get; set; } = string.Empty;
    public int IntersectionId { get; set; }
    public string IntersectionName { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}