using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserStore.Business.Subscribe;
using UserStore.Models.Requests;

namespace UserStore.Controllers;

[ApiController]
[Authorize]
[Route("api/users/subscriptions")]
public class UserSubscriptionController : ControllerBase
{
    private readonly IUserSubscriptionService _subscriptionService;
    private readonly ILogger<UserSubscriptionController> _logger;
    private const string domain = "[CONTROLLER][SUBSCRIPTION]";

    public UserSubscriptionController(
        IUserSubscriptionService subscriptionService,
        ILogger<UserSubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpPost]
    [Route("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] UserSubscriptionRequest request)
    {
        if (string.IsNullOrEmpty(request.Intersection) || string.IsNullOrEmpty(request.Metric))
            return BadRequest("Intersection and metric are required.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

        _logger.LogInformation("{domain}[SUBSCRIBE] Received subscription request from {UserId} ({Email})\n", domain, userId, email);

        var result = await _subscriptionService.SubscribeAsync(userId, email, request);
        return Ok(result);
    }
}
