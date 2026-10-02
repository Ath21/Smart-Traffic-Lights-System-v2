using System.Net;
using System.Net.Sockets;
using IntersectionControllerStore.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntersectionControllerStore.Controllers.Healthchecks
{
    [ApiController]
    [Route("/")]
    public class HealthController : ControllerBase
    {
        private readonly IntersectionContext _intersection;
        private readonly string _service;
        private readonly string _layer;
        private readonly string _level;
        private readonly string _environment;
        private readonly string _hostname;
        private readonly string _containerIp;

        public HealthController(IntersectionContext intersection)
        {
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
        [Route("health")]        
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                service = _service,
                layer = _layer,
                level = _level,
                environment = _environment,
                hostname = _hostname,
                container_ip = _containerIp,
                intersection = new { _intersection.Id, _intersection.Name },
                timestamp = DateTime.UtcNow.ToString("u")
            });
        }
    }
}