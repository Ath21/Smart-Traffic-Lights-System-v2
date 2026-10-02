using Messages.Sensor;
using Messages.Sensor.Detection;

namespace DetectionStore.Publishers.Event;

public interface IDetectionEventPublisher
{
    Task PublishEmergencyVehicleDetectedAsync(EmergencyVehicleDetectedMessage message);
    Task PublishPublicTransportDetectedAsync(PublicTransportDetectedMessage message);
    Task PublishIncidentDetectedAsync(IncidentDetectedMessage message);
}
