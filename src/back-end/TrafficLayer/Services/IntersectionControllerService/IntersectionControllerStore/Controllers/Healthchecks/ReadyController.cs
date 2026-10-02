using System.Net;
using System.Net.Sockets;
using DetectionCacheData;
using IntersectionControllerStore.Domain;
using MassTransit;
using Messages.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrafficLightCacheData;

namespace IntersectionControllerStore.Controllers.Healthchecks
{
    [ApiController]
    [Route("/")]
    public class ReadyController : ControllerBase
    {
        private readonly TrafficLightCacheDbContext _trafficLightCacheDbContext;
        private readonly DetectionCacheDbContext _detectionCacheDbContext;
        private readonly IBusControl _bus;
        private readonly IntersectionContext _intersection;

        private readonly string _service;
        private readonly string _layer;
        private readonly string _level;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public ReadyController(
            TrafficLightCacheDbContext trafficLightCacheDbContext,
            DetectionCacheDbContext detectionCacheDbContext,
            IBusControl bus,
            IntersectionContext intersection)
        {
            _trafficLightCacheDbContext = trafficLightCacheDbContext;
            _detectionCacheDbContext = detectionCacheDbContext;
            _bus = bus;
            _intersection = intersection;

            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "IntersectionController";
            _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Traffic";
            _level = Environment.GetEnvironmentVariable("SERVICE_LEVEL") ?? "Fog";
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            _hostname = Environment.MachineName;
            _containerIp = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString() ?? "unknown";
        }

        [HttpGet]
        [Route("ready")]
        public async Task<IActionResult> Ready()
        {
            var status = new Dictionary<string, object?>
            {
                ["status"] = "Ready",
                ["service"] = _service,
                ["layer"] = _layer,
                ["level"] = _level,
                ["environment"] = _environment,
                ["hostname"] = _hostname,
                ["container_ip"] = _containerIp,
                ["intersection"] = new { _intersection.Id, _intersection.Name },
                ["timestamp"] = DateTime.UtcNow.ToString("u")
            };

            try
            {
                // ---- TrafficLight Cache Redis ----
                bool trafficCacheOk = await _trafficLightCacheDbContext.CanConnectAsync();
                status["traffic_light_cache"] = new { name = "TrafficLightCacheDB (Redis)", reachable = trafficCacheOk };
                if (!trafficCacheOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "TrafficLightCacheDB (Redis) unreachable";
                    return StatusCode(503, status);
                }

                // ---- Detection Cache Redis ----
                bool detectionCacheOk = await _detectionCacheDbContext.CanConnectAsync();
                status["detection_cache"] = new { name = "DetectionCacheDB (Redis)", reachable = detectionCacheOk };
                if (!detectionCacheOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "DetectionCacheDB (Redis) unreachable";
                    return StatusCode(503, status);
                }

                // ---- RabbitMQ ----
                bool brokerOk = _bus.Topology.TryGetPublishAddress(typeof(LogMessage), out _);
                status["message_broker"] = new { name = "RabbitMQ", reachable = brokerOk };
                if (!brokerOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "RabbitMQ not connected";
                    return StatusCode(503, status);
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                status["status"] = "Not Ready";
                status["error"] = ex.Message;
                return StatusCode(503, status);
            }
        }
    }
}