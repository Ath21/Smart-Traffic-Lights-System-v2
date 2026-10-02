using System.Net;
using System.Net.Sockets;
using DetectionData;
using MassTransit;
using Messages.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrafficAnalyticsData;

namespace TrafficAnalyticsStore.Controllers.Healthchecks
{
    [ApiController]
    [Route("/")]
    public class ReadyController : ControllerBase
    {
        private readonly TrafficAnalyticsDbContext _dbContext;
        private readonly DetectionDbContext _detectionDbContext;
        private readonly IBusControl _bus;

        private readonly string _service;
        private readonly string _layer;
        private readonly string _level;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public ReadyController(
            TrafficAnalyticsDbContext dbContext,
            DetectionDbContext detectionDbContext,
            IBusControl bus)
        {
            _dbContext = dbContext;
            _detectionDbContext = detectionDbContext;
            _bus = bus;

            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "TrafficAnalytics";
            _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Traffic";
            _level = Environment.GetEnvironmentVariable("SERVICE_LEVEL") ?? "Cloud";
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
                ["timestamp"] = DateTime.UtcNow.ToString("u")
            };

            try
            {
                // ---- TrafficAnalytics PostgreSQL ----
                bool trafficDbConnected = await _dbContext.CanConnectAsync();
                status["traffic-analytics-db"] = new { name = "TrafficAnalyticsDB (PostgreSQL)", reachable = trafficDbConnected };
                if (!trafficDbConnected)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "TrafficAnalyticsDB PostgreSQL unreachable";
                    return StatusCode(503, status);
                }

                // ---- Detection MongoDB ----
                bool detectionDbConnected = await _detectionDbContext.CanConnectAsync();
                status["detection-db"] = new { name = "DetectionDB (MongoDB)", reachable = detectionDbConnected };
                if (!detectionDbConnected)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "DetectionDB MongoDB unreachable";
                    return StatusCode(503, status);
                }

                // ---- RabbitMQ ----
                bool brokerConnected = _bus.Topology.TryGetPublishAddress(typeof(LogMessage), out _);
                status["rabbitmq"] = new { name = "RabbitMQ", reachable = brokerConnected };
                if (!brokerConnected)
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
