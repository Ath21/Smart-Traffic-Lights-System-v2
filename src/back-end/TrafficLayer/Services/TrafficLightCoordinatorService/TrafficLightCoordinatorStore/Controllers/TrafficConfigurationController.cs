using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrafficLightData;

namespace TrafficLightCoordinatorStore.Controllers;

[ApiController]
[Route("api/configurations")]
public class TrafficConfigurationsController : ControllerBase
{
    private readonly TrafficLightDbContext _db;
    private readonly ILogger<TrafficConfigurationsController> _logger;
    private const string domain = "[CONTROLLER][TRAFFIC_CONFIGURATION]";

    public TrafficConfigurationsController(TrafficLightDbContext db, ILogger<TrafficConfigurationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: /api/configurations
    [HttpGet]
    [Route("all")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("{Domain}[GET_ALL] GetAll called\n", domain);

        var configs = await _db.TrafficConfigurations
            .AsNoTracking()
            .OrderBy(c => c.ConfigurationId)
            .Select(c => new
            {
                c.ConfigurationId,
                c.Mode,
                c.CycleDurationSec,
                c.GlobalOffsetSec,
                PhaseDurations = c.PhaseDurationsJson,
                c.Purpose,
                c.LastUpdated
            })
            .ToListAsync();

        return Ok(configs);
    }

    // GET: /api/configurations/{mode}
    [HttpGet]
    [Route("mode")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> GetByMode(string mode)
    {
        _logger.LogInformation("{Domain}[GET_BY_MODE] GetByMode called with mode={Mode}\n", domain, mode);

        var config = await _db.TrafficConfigurations
            .AsNoTracking()
            .Where(c => c.Mode.ToLower() == mode.ToLower())
            .Select(c => new
            {
                c.ConfigurationId,
                c.Mode,
                c.CycleDurationSec,
                c.GlobalOffsetSec,
                PhaseDurations = c.PhaseDurationsJson,
                c.Purpose,
                c.LastUpdated
            })
            .FirstOrDefaultAsync();

        if (config is null)
            return NotFound(new { error = $"Mode '{mode}' not found" });

        return Ok(config);
    }
}
