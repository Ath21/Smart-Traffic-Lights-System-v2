using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrafficLightCoordinatorStore.Business.Operator;
using TrafficLightCoordinatorStore.Models.Requests;

namespace TrafficLightCoordinatorStore.Controllers;

[ApiController]
[Route("api/traffic-operator")]
public class TrafficOperatorController : ControllerBase
{
    private readonly ITrafficOperatorBusiness _service;
    private readonly ILogger<TrafficOperatorController> _logger;
    private const string domain = "[CONTROLLER][TRAFFIC_OPERATOR]";

    public TrafficOperatorController(
        ITrafficOperatorBusiness service,
        ILogger<TrafficOperatorController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [Route("apply-mode")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> ApplyMode([FromBody] ApplyModeRequest request)
    {
        try
        {
            _logger.LogInformation("{Domain}[APPLY_MODE] ApplyMode called with request: {@Request}\n", domain, request);
            await _service.ApplyModeAsync(request.IntersectionId, request.Mode);
            return Ok(new { Message = $"Mode '{request.Mode}' applied to intersection {request.IntersectionId}" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "{Domain}[APPLY_MODE] Failed to apply mode\n", domain);
            return NotFound(new { Error = ex.Message });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "{Domain}[APPLY_MODE] Unexpected error applying mode\n", domain);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost]
    [Route("override-light")]
    [Authorize(Roles = "Admin,TrafficOperator")]
    public async Task<IActionResult> OverrideLight([FromBody] OverrideLightRequest request)
    {
        try
        {
            _logger.LogInformation("{Domain}[OVERRIDE_LIGHT] OverrideLight called with request: {@Request}\n", domain, request);

            await _service.OverrideLightAsync(
                request.IntersectionId,
                request.LightId,
                request.Mode,
                request.PhaseDurations,
                request.RemainingTimeSec,
                request.CycleDurationSec,
                request.LocalOffsetSec,
                request.CycleProgressSec,
                request.PriorityLevel,
                request.IsFailoverActive
            );

            return Ok(new
            {
                Message = $"Override applied to light {request.LightId} on intersection {request.IntersectionId} with mode '{request.Mode}'"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "{Domain}[OVERRIDE_LIGHT] Failed to override light\n", domain);
            return NotFound(new { Error = ex.Message });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "{Domain}[OVERRIDE_LIGHT] Unexpected error overriding light\n", domain);
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}