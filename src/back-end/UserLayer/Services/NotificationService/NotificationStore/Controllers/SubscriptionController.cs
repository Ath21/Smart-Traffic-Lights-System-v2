using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationStore.Business.Subscription;
using NotificationStore.Models.Requests;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Controllers;

[ApiController]
[Route("api/notifications/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly INotificationSubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionController> _logger;
    private const string domain = "[CONTROLLER][SUBSCRIPTION]";

    public SubscriptionController(
        INotificationSubscriptionService subscriptionService,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet]
    [Route("{userId}")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptions(string userId)
    {
        _logger.LogInformation("{Domain}[GET_SUBSCRIPTIONS] GetSubscriptions called for userId: {UserId}\n", domain, userId);

        var result = await _subscriptionService.GetUserSubscriptionsAsync(userId);
        return Ok(result);
    }

    [HttpDelete]
    [Route("unsubscribe")]
    [Authorize]
    public async Task<IActionResult> Unsubscribe([FromQuery] UnsubscribeRequest request)
    {
        _logger.LogInformation("{Domain}[UNSUBSCRIBE] Unsubscribe called for UserEmail: {UserEmail}\n",
            domain, request.UserEmail);
        
        if (string.IsNullOrEmpty(request.UserEmail))
            return BadRequest("UserEmail is required.");

        await _subscriptionService.UnsubscribeAsync(request);

        return NoContent();
    }
}
