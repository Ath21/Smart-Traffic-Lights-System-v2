// priority.detection.{intersection}.{event}
//
// {intersection} : agiou-spyridonos, anatoliki-pyli, dytiki-pyli, ekklisia, kentriki-pyli
// {event}        : emergency, incident, public-transport
//
// Published by : Intersection Controller Service
// Consumed by  : Traffic Light Coordinator Service
namespace Messages.Traffic.Priority;

public class PriorityEventMessage 
{
  public int IntersectionId { get; set; }
  public string? IntersectionName { get; set; }
  public string? EventType { get; set; }
  public string? VehicleType { get; set; }
  public int PriorityLevel { get; set; }  // 1 (Low), 2 (Medium), 3 (High)
  public string? Direction { get; set; }
}

/*

priority.detection.kentriki-pyli.emergency

{
  "CorrelationId": "9f36ebea-8825-4cb0-a1b7-6cb89e44e00e",
  "Timestamp": "2025-10-07T14:32:05Z",
  "SourceServices": ["Intersection Controller Service"],
  "DestinationServices": ["Traffic Light Coordinator Service"],
  "IntersectionId": 5,
  "IntersectionName": "Kentriki Pyli",
  "EventType": "emergency",
  "VehicleType": "ambulance",
  "PriorityLevel": 3,
  "Direction": "south",
  "Metadata": {
    "speed_kmh": "62.4",
    "eta_sec": "10",
    "sensor_source": "detection-worker-south"
  }
}

*/