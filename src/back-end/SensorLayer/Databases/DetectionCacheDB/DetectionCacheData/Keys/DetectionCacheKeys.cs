using System;

namespace DetectionCacheData.Keys;

public static class DetectionCacheKeys
{
    public static string VehicleCount(int intersectionId)
        => $"sensor:{intersectionId}:vehicle_count";
    public static string PedestrianCount(int intersectionId)
        => $"sensor:{intersectionId}:pedestrian_count";
    public static string CyclistCount(int intersectionId)
        => $"sensor:{intersectionId}:cyclist_count";
    public static string EmergencyDetected(int intersectionId)
        => $"detection:{intersectionId}:emergency_vehicle_detected";
    public static string PublicTransportDetected(int intersectionId)
        => $"detection:{intersectionId}:public_transport_detected";
    public static string IncidentDetected(int intersectionId)
        => $"detection:{intersectionId}:incident_detected";
}
