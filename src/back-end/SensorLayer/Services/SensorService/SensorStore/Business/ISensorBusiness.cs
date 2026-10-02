using System;
using Messages.Sensor;
using Messages.Sensor.Count;
using SensorStore.Models.Requests;
using SensorStore.Models.Responses;

namespace SensorStore.Business;

public interface ISensorBusiness
{
    Task ProcessVehicleCountAsync(VehicleCountMessage msg);
    Task ProcessPedestrianCountAsync(PedestrianCountMessage msg);
    Task ProcessCyclistCountAsync(CyclistCountMessage msg);

    Task<IEnumerable<VehicleCountResponse>> GetRecentVehicleCountsAsync(int intersectionId);
    Task<IEnumerable<PedestrianCountResponse>> GetRecentPedestrianCountsAsync(int intersectionId);
    Task<IEnumerable<CyclistCountResponse>> GetRecentCyclistCountsAsync(int intersectionId);
}
