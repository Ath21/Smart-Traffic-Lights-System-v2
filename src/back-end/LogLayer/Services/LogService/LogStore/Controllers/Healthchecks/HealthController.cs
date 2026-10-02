using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogStore.Controllers
{
    [ApiController]
    [Route("/")]
    public class HealthController : ControllerBase
    {
        private readonly string _layer;
        private readonly string _level;
        private readonly string _service;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public HealthController()
        {
            _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Log";
            _level = Environment.GetEnvironmentVariable("SERVICE_LEVEL") ?? "Cloud";
            _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "Log Service";
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            _hostname = Environment.MachineName;
            _containerIp = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString() ?? "unknown";
        }

        [HttpGet]
        [Route("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                service = _service,
                environment = _environment,
                layer = _layer,
                level = _level,
                hostname = _hostname,
                container_ip = _containerIp,
                timestamp = DateTime.UtcNow.ToString("u")
            });
        }
    }
}
