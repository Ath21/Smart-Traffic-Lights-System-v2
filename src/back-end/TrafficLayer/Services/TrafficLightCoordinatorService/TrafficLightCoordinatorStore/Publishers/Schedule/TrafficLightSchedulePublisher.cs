using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Messages.Traffic.Light;

namespace TrafficLightCoordinatorStore.Publishers.Schedule
{
    public class TrafficLightSchedulePublisher : ITrafficLightSchedulePublisher
    {
        private readonly IBus _bus;
        private readonly ILogger<TrafficLightSchedulePublisher> _logger;
        private readonly string _routingPattern;

        public TrafficLightSchedulePublisher(IBus bus, IConfiguration config, ILogger<TrafficLightSchedulePublisher> logger)
        {
            _bus = bus;
            _logger = logger;

            _routingPattern = config["RabbitMQ:RoutingKeys:Traffic:LightSchedule"]
                              ?? "traffic.light.schedule.{intersection}";
        }

        // ============================================================
        // PUBLISH UPDATED SCHEDULE
        // ============================================================
        public async Task PublishUpdateAsync(
            int intersectionId,
            string intersectionName,
            bool isOperational,
            string currentMode,
            Dictionary<string, int> phaseDurations,
            int cycleDurationSec,
            int globalOffsetSec,
            string? purpose = null)
        {
            var msg = new TrafficLightScheduleMessage
            {
                IntersectionId = intersectionId,
                IntersectionName = intersectionName,
                IsOperational = isOperational,
                CurrentMode = currentMode,
                PhaseDurations = phaseDurations,
                CycleDurationSec = cycleDurationSec,
                GlobalOffsetSec = globalOffsetSec,
                Purpose = purpose,
                LastUpdate = DateTime.UtcNow
            };

            var routingKey = _routingPattern
                .Replace("{intersection}", intersectionName.ToLower().Replace(' ', '-'));

            await _bus.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));

            _logger.LogInformation(
                "[PUBLISHER][SCHEDULE][{Intersection}] Published light schedule (Mode={Mode}, Cycle={Cycle}s, Offset={Offset}s)",
                intersectionName, currentMode, cycleDurationSec, globalOffsetSec);
        }
    }
}
