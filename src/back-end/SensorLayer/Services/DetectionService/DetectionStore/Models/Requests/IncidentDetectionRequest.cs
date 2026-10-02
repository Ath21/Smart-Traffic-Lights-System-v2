using System;

namespace DetectionStore.Models.Requests;

public class IncidentDetectionRequest
{
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
}