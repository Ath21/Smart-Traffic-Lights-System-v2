using System;

namespace SensorStore.Models.Responses;

public class CyclistCountResponse
{
    public string CyclistId { get; set; } = string.Empty;
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}
