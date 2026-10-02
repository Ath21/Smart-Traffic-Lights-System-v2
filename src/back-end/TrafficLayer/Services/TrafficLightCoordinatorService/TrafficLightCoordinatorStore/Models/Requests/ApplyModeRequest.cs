using System;

namespace TrafficLightCoordinatorStore.Models.Requests;

public class ApplyModeRequest
{
    public int IntersectionId { get; set; }
    public string Mode { get; set; } = string.Empty;
}
