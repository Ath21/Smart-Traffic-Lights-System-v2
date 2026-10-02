using System;

namespace TrafficLightCoordinatorStore.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrafficLightData;

[ApiController]
[Route("api/intersections")]
public class IntersectionsController : ControllerBase
{
    private readonly TrafficLightDbContext _db;
    private readonly ILogger<IntersectionsController> _logger;
    private const string domain = "[CONTROLLER][INTERSECTION]";

    public IntersectionsController(TrafficLightDbContext db, ILogger<IntersectionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: /api/intersections
    [HttpGet]
    [Route("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("{Domain}[GET_ALL] GetAll called\n", domain);

        var intersections = await _db.Intersections
            .AsNoTracking()
            .Include(i => i.TrafficLights)
            .OrderBy(i => i.IntersectionId)
            .Select(i => new
            {
                i.IntersectionId,
                i.Name,
                i.Location,
                Coordinates = new { i.Latitude, i.Longitude },
                i.LightCount,
                i.IsActive,
                i.CreatedAt,
                TrafficLights = i.TrafficLights.Select(t => new
                {
                    t.LightId,
                    t.LightName,
                    t.Direction,
                    Coordinates = new { t.Latitude, t.Longitude },
                    t.IsOperational
                })
            })
            .ToListAsync();

        return Ok(intersections);
    }

    // GET: /api/intersections/{id}
    [HttpGet]
    [Route("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("{Domain}[GET_BY_ID] GetById called for Intersection ID {Id}\n", domain, id);

        var intersection = await _db.Intersections
            .AsNoTracking()
            .Include(i => i.TrafficLights)
            .FirstOrDefaultAsync(i => i.IntersectionId == id);

        if (intersection is null)
            return NotFound(new { error = $"Intersection ID {id} not found" });

        var response = new
        {
            intersection.IntersectionId,
            intersection.Name,
            intersection.Location,
            Coordinates = new { intersection.Latitude, intersection.Longitude },
            intersection.LightCount,
            intersection.IsActive,
            intersection.CreatedAt,
            TrafficLights = intersection.TrafficLights.Select(t => new
            {
                t.LightId,
                t.LightName,
                t.Direction,
                Coordinates = new { t.Latitude, t.Longitude },
                t.IsOperational
            })
        };

        return Ok(response);
    }
}

