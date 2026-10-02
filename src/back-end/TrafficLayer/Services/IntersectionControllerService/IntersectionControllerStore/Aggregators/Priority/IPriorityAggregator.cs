using System;
using Messages.Sensor.Count;
using Messages.Sensor.Detection;
using Messages.Traffic.Priority;

namespace IntersectionControllerStore.Aggregators.Priority;

public interface IPriorityAggregator
{
    Task<PriorityEventMessage?> BuildPriorityEventAsync(EmergencyVehicleDetectedMessage evMsg);
    Task<PriorityEventMessage?> BuildPriorityEventAsync(IncidentDetectedMessage incidentMsg);
    Task<PriorityEventMessage?> BuildPriorityEventAsync(PublicTransportDetectedMessage ptMsg);

    Task<PriorityCountMessage?> BuildPriorityCountAsync(VehicleCountMessage vehicleMsg);
    Task<PriorityCountMessage?> BuildPriorityCountAsync(PedestrianCountMessage pedestrianMsg);
    Task<PriorityCountMessage?> BuildPriorityCountAsync(CyclistCountMessage cyclistMsg);
}
