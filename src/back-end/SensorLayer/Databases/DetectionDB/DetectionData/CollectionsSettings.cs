using System;

namespace DetectionData;

public class CollectionsSettings
{
    public string? VehicleCount { get; set; } // = "vehicle_count";
    public string? PedestrianCount { get; set; } // = "pedestrian_count";
    public string? CyclistCount { get; set; } // = "cyclist_count";
    public string? PublicTransport { get; set; } // = "public_transport_detections";
    public string? EmergencyVehicle { get; set; } // = "emergency_vehicle_detections";
    public string? Incident { get; set; } // = "incident_detections";
}
