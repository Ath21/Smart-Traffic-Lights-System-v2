using System;

namespace TrafficLightCoordinatorStore.Models.Requests;

public class OverrideLightRequest
{
    public int IntersectionId { get; set; }
    public int LightId { get; set; }
    public string Mode { get; set; } = null!;
    public Dictionary<string, int>? PhaseDurations { get; set; }
    public int RemainingTimeSec { get; set; } = 0;
    public int CycleDurationSec { get; set; } = 60;
    public int LocalOffsetSec { get; set; } = 0;
    public double CycleProgressSec { get; set; } = 0;
    public int PriorityLevel { get; set; } = 1;
    public bool IsFailoverActive { get; set; } = false;
}
