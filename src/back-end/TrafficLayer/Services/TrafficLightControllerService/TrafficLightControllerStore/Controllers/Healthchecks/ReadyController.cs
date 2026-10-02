using System.Net;
using System.Net.Sockets;
using MassTransit;
using Messages.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrafficLightCacheData;
using TrafficLightControllerStore.Domain;

namespace TrafficLightControllerStore.Controllers.Healthchecks
{
    [ApiController]
    [Route("/")]
    public class ReadyController : ControllerBase
    {
        private readonly TrafficLightCacheDbContext _trafficLightCacheDbContext;
        private readonly IBusControl _bus;
        private readonly IntersectionContext _intersection;
        private readonly TrafficLightContext _light;

        private readonly string _service;
        private readonly string _layer;
        private readonly string _level;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public ReadyController(
            TrafficLightCacheDbContext trafficLightCacheDbContext,
            IBusControl bus,
            IntersectionContext intersection,
            TrafficLightContext light)
        {
            _trafficLightCacheDbContext = trafficLightCacheDbContext;
            _bus = bus;
            _intersection = intersection;
            _light = light;

            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "TrafficLightController";
            _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Traffic";
            _level = Environment.GetEnvironmentVariable("SERVICE_LEVEL") ?? "Edge";
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
                ["light"] = new { _light.Id, _light.Name },
                ["timestamp"] = DateTime.UtcNow.ToString("u")
            };

            try
            {
                // ---- TrafficLight Cache Redis ----
                bool cacheOk = await _trafficLightCacheDbContext.CanConnectAsync();
                status["traffic_light_cache"] = new { name = "TrafficLightCacheDB (Redis)", reachable = cacheOk };
                if (!cacheOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "TrafficLightCacheDB (Redis) unreachable";
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
