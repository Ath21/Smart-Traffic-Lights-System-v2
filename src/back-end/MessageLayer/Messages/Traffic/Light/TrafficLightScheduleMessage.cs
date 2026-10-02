using System;
using System.Collections.Generic;

namespace Messages.Traffic.Light;

public class TrafficLightScheduleMessage 
{
    public int IntersectionId { get; set; }
    public string? IntersectionName { get; set; }
    public bool IsOperational { get; set; } = true;
    public string CurrentMode { get; set; } = "Standard";
    public Dictionary<string, int> PhaseDurations { get; set; } = new();
    public int CycleDurationSec { get; set; }
    public int GlobalOffsetSec { get; set; }
    public string? Purpose { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}
