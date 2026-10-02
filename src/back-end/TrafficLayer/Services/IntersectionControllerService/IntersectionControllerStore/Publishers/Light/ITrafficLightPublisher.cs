using System;
using Messages.Traffic.Light;

namespace IntersectionControllerStore.Publishers.Light;

public interface ITrafficLightPublisher
{
    Task PublishLightControlAsync(TrafficLightControlMessage message);
}
