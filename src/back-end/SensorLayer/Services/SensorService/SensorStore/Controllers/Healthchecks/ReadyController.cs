using System.Net;
using System.Net.Sockets;
using DetectionCacheData;
using DetectionData;
using SensorStore.Domain;
using MassTransit;
using Messages.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SensorStore.Controllers.Healthchecks
{
    [ApiController]
    [Route("/")]
    public class ReadyController : ControllerBase
    {
        private readonly DetectionDbContext _detectionDbContext;
        private readonly DetectionCacheDbContext _detectionCacheDbContext;
        private readonly IBusControl _bus;
        private readonly IntersectionContext _intersection;

        private readonly string _layer;
        private readonly string _level;
        private readonly string _service;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public ReadyController(
            DetectionDbContext detectionDbContext,
            DetectionCacheDbContext detectionCacheDbContext,
            IBusControl bus,
            IntersectionContext intersection)
        {
            _detectionDbContext = detectionDbContext;
            _detectionCacheDbContext = detectionCacheDbContext;
            _bus = bus;
            _intersection = intersection;

            _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Sensor";
            _level = Environment.GetEnvironmentVariable("SERVICE_LEVEL") ?? "Fog";
            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "Sensor";
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
                // ---- MongoDB Check ----
                bool mongoOk = await _detectionDbContext.CanConnectAsync();
                status["mongodb"] = new { name = "DetectionDB (MongoDB)", reachable = mongoOk };
                if (!mongoOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "DetectionDB unreachable";
                    return StatusCode(503, status);
                }

                // ---- Redis Check ----
                bool redisOk = await _detectionCacheDbContext.CanConnectAsync();
                status["redis"] = new { name = "DetectionCacheDB (Redis)", reachable = redisOk };
                if (!redisOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "Redis unreachable";
                    return StatusCode(503, status);
                }

                // ---- RabbitMQ Check ----
                bool brokerOk = _bus.Topology.TryGetPublishAddress(typeof(LogMessage), out _);
                status["message_broker"] = new { name = "RabbitMQ", reachable = brokerOk };
                if (!brokerOk)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "RabbitMQ unreachable or topology not established";
                    return StatusCode(503, status);
                }

                // Everything OK
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
