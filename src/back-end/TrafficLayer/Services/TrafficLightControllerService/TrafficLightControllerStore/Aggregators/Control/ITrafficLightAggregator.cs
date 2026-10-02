using System;
using Messages.Traffic.Light;
using TrafficLightControllerStore.Aggregators.Time;

namespace TrafficLightControllerStore.Aggregators.Control;

public interface ITrafficLightAggregator
{
    Task ApplyControlMessageAsync(TrafficLightControlMessage msg);
    bool TryGetTimer(int intersectionId, int lightId, out ITrafficLightTimer? timer);
}