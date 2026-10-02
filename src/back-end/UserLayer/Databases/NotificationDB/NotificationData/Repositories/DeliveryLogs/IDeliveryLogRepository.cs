using NotificationData.Collections;

namespace NotificationData.Repositories.DeliveryLogs;

public interface IDeliveryLogRepository
{
    Task LogDeliveryAsync(string userId, string userEmail, string messageId, string status);
    Task<IEnumerable<DeliveryLogCollection>> GetUserDeliveriesAsync(string userId, bool unreadOnly = false);
    Task MarkAsReadAsync(string userId, string userEmail, string deliveryId);
}
