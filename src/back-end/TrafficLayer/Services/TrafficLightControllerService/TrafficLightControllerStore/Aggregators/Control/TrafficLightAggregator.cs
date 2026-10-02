using System;
using System.Collections.Concurrent;
using Messages.Traffic.Light;
using TrafficLightCacheData.Repositories;
using TrafficLightControllerStore.Aggregators.Time;

namespace TrafficLightControllerStore.Aggregators.Control;

public class TrafficLightAggregator : ITrafficLightAggregator
{
    private readonly ITrafficLightCacheRepository _cache;
    private readonly ConcurrentDictionary<(int intersectionId, int lightId), ITrafficLightTimer> _timers
        = new();

    public TrafficLightAggregator(ITrafficLightCacheRepository cache)
    {
        _cache = cache;
    }

    public async Task ApplyControlMessageAsync(TrafficLightControlMessage msg)
    {
        var key = (msg.IntersectionId, msg.LightId);

        if (!_timers.TryGetValue(key, out var timer))
        {
            timer = new TrafficLightTimer(_cache, msg.IntersectionId, msg.LightId);
            _timers[key] = timer;
        }

        timer.Start(msg);

        // Optionally update cache for mode / priority
        await _cache.SetModeAsync(msg.IntersectionId, msg.LightId, msg.Mode ?? "Unknown");
        await _cache.SetPriorityAsync(msg.IntersectionId, msg.LightId, msg.PriorityLevel);
        await _cache.SetFailoverAsync(msg.IntersectionId, msg.LightId, msg.IsFailoverActive);
        await _cache.SetStateAsync(msg.IntersectionId, msg.LightId, msg.OperationalMode ?? "Unknown");
    }

    public bool TryGetTimer(int intersectionId, int lightId, out ITrafficLightTimer? timer)
        => _timers.TryGetValue((intersectionId, lightId), out timer);
}
