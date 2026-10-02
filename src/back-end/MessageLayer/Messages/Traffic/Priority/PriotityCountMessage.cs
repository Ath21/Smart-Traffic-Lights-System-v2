// priority.count.{intersection}.{type}
// 
// {intersection} : agiou-spyridonos, anatoliki-pyli, dytiki-pyli, ekklisia, kentriki-pyli
// {type}         : vehicle, pedestrian, cyclist
//
// Published by : Intersection Controller Service
// Consumed by  : Traffic Light Coordinator Service
namespace Messages.Traffic.Priority;

public class PriorityCountMessage 
{
  public int IntersectionId { get; set; }
  public string? IntersectionName { get; set; }
  public string? CountType { get; set; }  // Vehicle, Pedestrian, Cyclist
  public int TotalCount { get; set; }
  public int PriorityLevel { get; set; }   // 1 (Low), 2 (Medium), 3 (High)
  public bool IsThresholdExceeded { get; set; } // True if congestion threshold exceeded
}

/*

priority.count.agiou-spyridonos.vehicle

{
  "CorrelationId": "b0a32f2e-94b4-4b1d-8f61-47e21a9cd07e",
  "Timestamp": "2025-10-07T14:28:22Z",
  "SourceServices": ["Intersection Controller Service"],
  "DestinationServices": ["Traffic Light Coordinator Service"],
  "IntersectionId": 2,
  "IntersectionName": "Agiou Spyridonos",
  "CountType": "Vehicle",
  "TotalCount": 128,
  "PriorityLevel": 3,
  "IsThresholdExceeded": true,
  "Metadata": {
    "average_wait_time_sec": "45.3",
    "flow_rate": "2.1",
    "threshold_limit": "100"
  }
}

*/