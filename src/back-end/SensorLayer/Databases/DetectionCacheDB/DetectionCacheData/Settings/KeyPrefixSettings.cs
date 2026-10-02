using System;

namespace DetectionCacheData.Settings;

public class KeyPrefixSettings
{
    public string? VehicleCount { get; set; } //= "vehicle_count";
    public string? PedestrianCount { get; set; } //= "pedestrian_count";
    public string? CyclistCount { get; set; } //= "cyclist_count";
    public string? PublicTransportDetections { get; set; } //= "public_transport_detections";
    public string? EmergencyVehicleDetections { get; set; } //= "emergency_vehicle_detections";
    public string? IncidentDetections { get; set; } //= "incident_detections";
}
