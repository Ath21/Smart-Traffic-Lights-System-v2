using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationStore.Business.Delivery;
using NotificationStore.Models.Requests;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Controllers;

[ApiController]
[Route("api/notifications/deliveries")]
public class DeliveryController : ControllerBase
{
    private readonly INotificationDeliveryService _deliveryService;
    private readonly ILogger<DeliveryController> _logger;
    private const string domain = "[CONTROLLER][DELIVERY]";

    public DeliveryController(
        INotificationDeliveryService deliveryService,
        ILogger<DeliveryController> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    [HttpGet]
    [Route("{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeliveries(string userId, [FromQuery] bool unreadOnly = false)
    {
        _logger.LogInformation(
            "{Domain}[GET_DELIVERIES] Delivery retrieval requested for UserId={UserId} | UnreadOnly={UnreadOnly}\n",
            domain, userId, unreadOnly);

        var logs = await _deliveryService.GetUserDeliveriesAsync(userId, unreadOnly);

        return Ok(logs);
    }

    [HttpPatch]
    [Route("read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
    {
        _logger.LogInformation(
            "{Domain}[MARK_AS_READ] Mark as read requested for DeliveryId={DeliveryId} by UserId={UserId}\n",
            domain, request.DeliveryId, request.UserId);

        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.UserEmail))
            return BadRequest("UserId and UserEmail are required.");

        await _deliveryService.MarkAsReadAsync(request);

        return Ok(new { status = "read", request.DeliveryId });
    }
}
