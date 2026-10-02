using Messages.Sensor.Count;

namespace SensorStore.Publishers.Count;

public interface ISensorCountPublisher
{
    Task PublishVehicleCountAsync(VehicleCountMessage message);
    Task PublishPedestrianCountAsync(PedestrianCountMessage message);
    Task PublishCyclistCountAsync(CyclistCountMessage message);
}
