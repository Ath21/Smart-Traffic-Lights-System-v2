using NotificationData.Repositories.DeliveryLogs;
using NotificationStore.Models.Requests;
using NotificationStore.Models.Responses;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Business.Delivery;

public class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly IDeliveryLogRepository _logRepo;
    private readonly INotificationLogPublisher _logPublisher;

    private const string Domain = "[BUSINESS][DELIVERY]";

    public NotificationDeliveryService(
        IDeliveryLogRepository logRepo,
        INotificationLogPublisher logPublisher)
    {
        _logRepo = logRepo;
        _logPublisher = logPublisher;
    }

    public async Task<IEnumerable<DeliveryResponse>> GetUserDeliveriesAsync(string userId, bool unreadOnly = false)
    {
        var logs = await _logRepo.GetUserDeliveriesAsync(userId, unreadOnly);

        await _logPublisher.PublishAuditAsync(
            domain: Domain,
            messageText: $"{Domain} Retrieved {(unreadOnly ? "unread" : "all")} deliveries for user {userId}",
            category: "DELIVERY",
            operation: "GetUserDeliveriesAsync",
            data: new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["UnreadOnly"] = unreadOnly,
                ["Count"] = logs.Count()
            });

        return logs.Select(l => new DeliveryResponse
        {
            DeliveryId = l.Id,
            UserId = l.UserId,
            UserEmail = l.UserEmail,
            MessageId = l.MessageId,
            DeliveredAt = l.DeliveredAt,
            Status = l.Status,
            Read = l.Status.Equals("Read", StringComparison.OrdinalIgnoreCase)
        });
    }

    public async Task MarkAsReadAsync(MarkAsReadRequest request)
    {
        await _logRepo.MarkAsReadAsync(request.UserId, request.UserEmail, request.DeliveryId);

        await _logPublisher.PublishAuditAsync(
            domain: Domain,
            messageText: $"{Domain} Marked delivery {request.DeliveryId} as read for {request.UserEmail}",
            category: "DELIVERY",
            operation: "MarkAsReadAsync",
            data: new Dictionary<string, object>
            {
                ["UserId"] = request.UserId,
                ["UserEmail"] = request.UserEmail,
                ["DeliveryId"] = request.DeliveryId
            });
    }
}
