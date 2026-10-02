using System;
using Messages.Traffic;
using Messages.Traffic.Priority;

namespace IntersectionControllerStore.Publishers.Priority;

public interface IPriorityPublisher
{
    Task PublishPriorityEventAsync(PriorityEventMessage message);
    Task PublishPriorityCountAsync(PriorityCountMessage message);
}
