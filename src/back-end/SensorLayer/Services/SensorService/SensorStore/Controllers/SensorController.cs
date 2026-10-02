using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorStore.Business;
using SensorStore.Domain;
using SensorStore.Models.Responses;

namespace SensorStore.Controllers;

[ApiController]
[Route("api/sensors")]
public class SensorController : ControllerBase
{
    private readonly ISensorBusiness _business;
    private readonly ILogger<SensorController> _logger;
    private readonly IntersectionContext _intersection;
    private const string domain = "[CONTROLLER][SENSOR]";

    public SensorController(
        ISensorBusiness business,
        ILogger<SensorController> logger,
        IntersectionContext intersection)
    {
        _business = business;
        _logger = logger;
        _intersection = intersection;
    }

    [HttpGet]
    [Route("vehicle")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetVehicleCounts()
    {
        var data = await _business.GetRecentVehicleCountsAsync(_intersection.Id);
        if (!data.Any())
        {
            _logger.LogWarning("{Domain}[VEHICLE] No vehicle counts found for intersection {Name}\n", domain, _intersection.Name);
            return NotFound();
        }

        _logger.LogInformation("{Domain}[VEHICLE] {Count} vehicle records returned for {Intersection}\n",
            domain, data.Count(), _intersection.Name);

        return Ok(data);
    }

    [HttpGet]
    [Route("pedestrian")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetPedestrianCounts()
    {
        var data = await _business.GetRecentPedestrianCountsAsync(_intersection.Id);
        if (!data.Any())
        {
            _logger.LogWarning("{Domain}[PEDESTRIAN] No pedestrian counts found for intersection {Name}\n", domain, _intersection.Name);
            return NotFound();
        }

        _logger.LogInformation("{Domain}[PEDESTRIAN] {Count} pedestrian records returned for {Intersection}\n",
            domain, data.Count(), _intersection.Name);

        return Ok(data);
    }

    [HttpGet]
    [Route("cyclist")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetCyclistCounts()
    {
        var data = await _business.GetRecentCyclistCountsAsync(_intersection.Id);
        if (!data.Any())
        {
            _logger.LogWarning("{Domain}[CYCLIST] No cyclist counts found for intersection {Name}\n", domain, _intersection.Name);
            return NotFound();
        }

        _logger.LogInformation("{Domain}[CYCLIST] {Count} cyclist records returned for {Intersection}\n",
            domain, data.Count(), _intersection.Name);

        return Ok(data);
    }
}
