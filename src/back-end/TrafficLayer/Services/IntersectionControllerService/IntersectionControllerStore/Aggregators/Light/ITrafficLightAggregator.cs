using System;
using IntersectionControllerStore.Domain;
using Messages.Traffic.Light;

namespace IntersectionControllerStore.Aggregators.Light;

public interface ITrafficLightAggregator
{
    Task<TrafficLightControlMessage> BuildLightControlAsync(
            TrafficLightScheduleMessage schedule,
            TrafficLightContext light,
            int lightIndex = 0,
            int totalLights = 1);
}