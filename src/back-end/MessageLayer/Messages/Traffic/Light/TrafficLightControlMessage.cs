// traffic.light.control.{intersection}.{light}
//
// {intersection} : agiou-spyridonos, anatoliki-pyli, dytiki-pyli, ekklisia, kentriki-pyli
// {light}        : agiou-spyridonos101, dimitsanas102, 
//                  anatoliki-pyli201, agiou-spyridonos202, 
//                  dytiki-pyli301, dimitsanas-north302, dimitsanas-south303, 
//                  dimitsanas401, edessis402, korytsas403, 
//                  kentriki-pyli501, agiou-spyridonos502
//
// Published by : Intersection Controller Service, User Service
// Consumed by  : Traffic Light Controller Service
namespace Messages.Traffic.Light;

public class TrafficLightControlMessage 
{
    public int IntersectionId { get; set; }
    public string? IntersectionName { get; set; }

    public int LightId { get; set; }
    public string? LightName { get; set; }

    public string? Mode { get; set; }              // Standard, Peak, Night, etc.
    public string? TimePlan { get; set; }          // Day, RushHour, Holiday
    public string? OperationalMode { get; set; }   // Normal, Flashing, Off

    public Dictionary<string, int>? PhaseDurations { get; set; }  // { "Green": 30, "Yellow": 5, "Red": 25 }
    public string? CurrentPhase { get; set; }      // Green, Yellow, Red
    public int RemainingTimeSec { get; set; }      // Remaining time in current phase
    public int CycleDurationSec { get; set; }      // Full cycle duration
    public int LocalOffsetSec { get; set; }        // Offset within intersection cycle
    public double CycleProgressSec { get; set; }   // Time elapsed in current cycle
    public int PriorityLevel { get; set; }         // 1–3 priority indicator
    public bool IsFailoverActive { get; set; }     // Indicates local failover active
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}


/*

traffic.light.control.agioy-spyridonos.agiou-spyridonos101

{
  "CorrelationId": "c98e1c40-4e5b-4a94-a0e4-bbbd7333f8ff",
  "Timestamp": "2025-10-08T07:30:01Z",
  "SourceServices": ["Intersection Controller Service"],
  "DestinationServices": ["Traffic Light Controller Service"],
  "IntersectionId": 2,
  "IntersectionName": "Agiou Spyridonos",
  "LightId": 101,
  "LightName": "agiou-spyridonos101",
  "OperationalMode": "Peak",
  "PhaseDurations": { "Green": 40, "Yellow": 5, "Red": 15 },
  "Metadata": {
    "cycle_duration": "60",
    "global_offset": "10",
    "light_offset": "0"
  }
}

traffic.light.control.agioy-spyridonos.agiou-spyridonos102

{
  "CorrelationId": "c98e1c40-4e5b-4a94-a0e4-bbbd7333f8ff",
  "Timestamp": "2025-10-08T07:30:01Z",
  "SourceServices": ["Intersection Controller Service"],
  "DestinationServices": ["Traffic Light Controller Service"],
  "IntersectionId": 2,
  "IntersectionName": "Agiou Spyridonos",
  "LightId": 102,
  "LightName": "agiou-spyridonos102",
  "OperationalMode": "Peak",
  "PhaseDurations": { "Green": 40, "Yellow": 5, "Red": 15 },
  "Metadata": {
    "cycle_duration": "60",
    "global_offset": "10",
    "light_offset": "5"
  }
}

traffic.light.control.agioy-spyridonos.agiou-spyridonos103

{
  "CorrelationId": "c98e1c40-4e5b-4a94-a0e4-bbbd7333f8ff",
  "Timestamp": "2025-10-08T07:30:01Z",
  "SourceServices": ["Intersection Controller Service"],
  "DestinationServices": ["Traffic Light Controller Service"],
  "IntersectionId": 2,
  "IntersectionName": "Agiou Spyridonos",
  "LightId": 103,
  "LightName": "agiou-spyridonos103",
  "OperationalMode": "Peak",
  "PhaseDurations": { "Green": 40, "Yellow": 5, "Red": 15 },
  "Metadata": {
    "cycle_duration": "60",
    "global_offset": "10",
    "light_offset": "10"
  }
}


*/