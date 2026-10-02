using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrafficLightData;
using Messages.Log;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;

namespace TrafficLightCoordinatorStore.Controllers.Healthchecks
{
    [ApiController]
    [Route("/")]
    public class ReadyController : ControllerBase
    {
        private readonly TrafficLightDbContext _dbContext;
        private readonly IBusControl _bus;

        private readonly string _service;
        private readonly string _layer;
        private readonly string _level;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public ReadyController(
            TrafficLightDbContext dbContext,
            IBusControl bus)
        {
            _dbContext = dbContext;
            _bus = bus;

            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "TrafficLightCoordinator";
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
                // ---- TrafficLight MSSQL ----
                bool dbConnected = await _dbContext.CanConnectAsync();
                status["traffic_light_db"] = new { name = "TrafficLightDB (MSSQL)", reachable = dbConnected };
                if (!dbConnected)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "TrafficLightDB MSSQL unreachable";
                    return StatusCode(503, status);
                }

                // ---- RabbitMQ ----
                bool brokerConnected = _bus.Topology.TryGetPublishAddress(typeof(LogMessage), out _);
                status["message_broker"] = new { name = "RabbitMQ", reachable = brokerConnected };
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
