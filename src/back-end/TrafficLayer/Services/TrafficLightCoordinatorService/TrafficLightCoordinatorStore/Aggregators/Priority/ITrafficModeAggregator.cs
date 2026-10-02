using System;
using Messages.Traffic.Priority;

namespace TrafficLightCoordinatorStore.Aggregators.Priority;

public interface ITrafficModeAggregator
{
    Task HandlePriorityCountAsync(PriorityCountMessage msg);
    Task HandlePriorityEventAsync(PriorityEventMessage msg);
}
