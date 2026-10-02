using DetectionStore.Models.Requests;
using DetectionStore.Models.Responses;
using MassTransit.Logging;
using Messages.Log;
using Messages.Sensor;
using Messages.Sensor.Detection;

namespace DetectionStore.Business;

public interface IDetectionBusiness
{
    Task ProcessEmergencyVehicleAsync(EmergencyVehicleDetectedMessage msg);
    Task ProcessPublicTransportAsync(PublicTransportDetectedMessage msg);
    Task ProcessIncidentAsync(IncidentDetectedMessage msg);

    Task<IEnumerable<EmergencyVehicleDetectionResponse>> GetRecentEmergenciesAsync(int intersectionId);
    Task<IEnumerable<PublicTransportDetectionResponse>> GetRecentPublicTransportsAsync(int intersectionId);
    Task<IEnumerable<IncidentDetectionResponse>> GetRecentIncidentsAsync(int intersectionId);
}
