using System;
using Messages.Traffic.Light;

namespace TrafficLightControllerStore.Aggregators.Time;


public interface ITrafficLightTimer
{
    void Start(TrafficLightControlMessage msg);
    void Stop();
}

