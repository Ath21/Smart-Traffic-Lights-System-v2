using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Messages.Traffic.Light;
using TrafficLightCacheData.Repositories;
using IntersectionControllerStore.Aggregators.Light;
using IntersectionControllerStore.Domain; // for IntersectionContext

namespace IntersectionControllerStore.Consumers.Light;

public class LightScheduleConsumer : IConsumer<TrafficLightScheduleMessage>
{
    private readonly ITrafficLightAggregator _aggregator;
    private readonly ITrafficLightCacheRepository _cache;
    private readonly IntersectionContext _intersection;
    private readonly ILogger<LightScheduleConsumer> _logger;

    public LightScheduleConsumer(
        ITrafficLightAggregator aggregator,
        ITrafficLightCacheRepository cache,
        IntersectionContext intersection,
        ILogger<LightScheduleConsumer> logger)
    {
        _aggregator = aggregator;
        _cache = cache;
        _intersection = intersection;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TrafficLightScheduleMessage> context)
    {
        var schedule = context.Message;

        if (schedule.IntersectionId != _intersection.Id)
        {
            _logger.LogWarning(
                "Received schedule for intersection {Id}, but this controller manages intersection {LocalId}. Ignoring.",
                schedule.IntersectionId, _intersection.Id);
            return;
        }

        _logger.LogInformation(
            "[Intersection {Id}] Received schedule from coordinator (Mode={Mode}, Cycle={Cycle}s, Offset={Offset}s)",
            schedule.IntersectionId, schedule.CurrentMode, schedule.CycleDurationSec, schedule.GlobalOffsetSec);

        await UpdateIntersectionCacheAsync(schedule);

        // For each light in this intersection, build and send control message
        for (int i = 0; i < _intersection.Lights.Count; i++)
        {
            var light = _intersection.Lights[i];
            await _aggregator.BuildLightControlAsync(
                schedule,
                light,
                lightIndex: i,
                totalLights: _intersection.Lights.Count
            );
        }

    }

    private async Task UpdateIntersectionCacheAsync(TrafficLightScheduleMessage schedule)
    {
        try
        {
            await _cache.SetCoordinatorSyncAsync(schedule.IntersectionId);
            await _cache.SetModeAsync(schedule.IntersectionId, 0, schedule.CurrentMode);
            await _cache.SetCycleDurationAsync(schedule.IntersectionId, 0, schedule.CycleDurationSec);
            await _cache.SetOffsetAsync(schedule.IntersectionId, 0, schedule.GlobalOffsetSec);

            if (schedule.PhaseDurations?.Count > 0)
                await _cache.SetCachedPhasesAsync(schedule.IntersectionId, 0, schedule.PhaseDurations);

            _logger.LogDebug(
                "[Intersection {Id}] Cache updated with mode={Mode}, cycle={Cycle}s, offset={Offset}s",
                schedule.IntersectionId, schedule.CurrentMode, schedule.CycleDurationSec, schedule.GlobalOffsetSec);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Intersection {Id}] Failed to update intersection cache during schedule sync.",
                schedule.IntersectionId);
        }
    }
}
