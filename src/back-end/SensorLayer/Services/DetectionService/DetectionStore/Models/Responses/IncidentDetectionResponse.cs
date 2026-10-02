using System;

namespace DetectionStore.Models.Responses;

public class IncidentDetectionResponse
{
    public string IncidentId { get; set; } = string.Empty;
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
}
