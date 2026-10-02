using Microsoft.AspNetCore.Mvc;
using DetectionStore.Business;
using DetectionStore.Domain;
using Microsoft.AspNetCore.Authorization;

namespace DetectionStore.Controllers;

[ApiController]
[Route("api/detections")]
public class DetectionController : ControllerBase
{
    private readonly IDetectionBusiness _business;
    private readonly ILogger<DetectionController> _logger;
    private readonly IntersectionContext _intersection;
    private const string domain = "[CONTROLLER][DETECTION]";

    public DetectionController(
        IDetectionBusiness business,
        ILogger<DetectionController> logger,
        IntersectionContext intersection)
    {
        _business = business;
        _logger = logger;
        _intersection = intersection;
    }

    [HttpGet]
    [Route("emergency")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetRecentEmergencies()
    {
        var data = await _business.GetRecentEmergenciesAsync(_intersection.Id);
        if (!data.Any())
        {
            _logger.LogWarning("{Domain}[EMERGENCY] No emergency detections found for {Intersection}\n", domain, _intersection.Name);
            return NotFound();
        }

        _logger.LogInformation("{Domain}[EMERGENCY] {Count} emergency detections returned for {Intersection}\n", domain, data.Count(), _intersection.Name);
        return Ok(data);
    }

    [HttpGet]
    [Route("public-transport")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetPublicTransports()
    {
        var data = await _business.GetRecentPublicTransportsAsync(_intersection.Id);
        if (!data.Any())
        {
            _logger.LogWarning("{Domain}[PUBLIC_TRANSPORT] No public transport detections found for {Intersection}\n", domain, _intersection.Name);
            return NotFound();
        }

        _logger.LogInformation("{Domain}[PUBLIC_TRANSPORT] {Count} public transport detections returned for {Intersection}\n", domain, data.Count(), _intersection.Name);
        return Ok(data);
    }

    [HttpGet]
    [Route("incident")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetRecentIncidents()
    {
        var data = await _business.GetRecentIncidentsAsync(_intersection.Id);
        if (!data.Any())
        {
            _logger.LogWarning("{Domain}[INCIDENT] No incident detections found for {Intersection}\n", domain, _intersection.Name);
            return NotFound();
        }

        _logger.LogInformation("{Domain}[INCIDENT] {Count} incident detections returned for {Intersection}\n", domain, data.Count(), _intersection.Name);
        return Ok(data);
    }
}
