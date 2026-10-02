using System;

namespace SensorStore.Models.Requests;

public class PedestrianCountRequest
{
    public int IntersectionId { get; set; }
    public string Intersection { get; set; } = string.Empty;
    public int Count { get; set; }
}
