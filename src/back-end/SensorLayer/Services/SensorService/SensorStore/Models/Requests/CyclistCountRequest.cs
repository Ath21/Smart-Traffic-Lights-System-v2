using System;

namespace SensorStore.Models.Requests;

public class CyclistCountRequest
{
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public int Count { get; set; }
}
