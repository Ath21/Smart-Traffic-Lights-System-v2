using System.Net;
using System.Net.Sockets;
using LogData;
using MassTransit;
using Messages.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogStore.Controllers
{
    [ApiController]
    [Route("/")]
    public class ReadyController : ControllerBase
    {
        private readonly LogDbContext _dbContext;
        private readonly IBusControl _bus;

        private readonly string _service;
        private readonly string _layer;
        private readonly string _level;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public ReadyController(LogDbContext dbContext, IBusControl bus)
        {
            _dbContext = dbContext;
            _bus = bus;

            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "Log";
            _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Log";
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
                // ===== MongoDB Connectivity =====
                bool dbConnected = await _dbContext.CanConnectAsync();
                status["database"] = new { name = "LogDB (MongoDB)", reachable = dbConnected };

                if (!dbConnected)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "MongoDB unreachable";
                    return StatusCode(503, status);
                }

                // ===== RabbitMQ Connectivity =====
                bool brokerConnected = _bus.Topology.TryGetPublishAddress(typeof(LogMessage), out _);
                status["message_broker"] = new { name = "RabbitMQ", reachable = brokerConnected };

                if (!brokerConnected)
                {
                    status["status"] = "Not Ready";
                    status["reason"] = "RabbitMQ unreachable or topology not established";
                    return StatusCode(503, status);
                } 
                
                // ===== OK =====
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
