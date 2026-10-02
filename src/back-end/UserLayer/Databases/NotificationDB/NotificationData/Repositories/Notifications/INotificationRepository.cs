using NotificationData.Collections;

namespace NotificationData.Repositories.Notifications;


public interface INotificationRepository
{
    Task AddOrUpdateSubscriptionAsync(NotificationCollection subscription);
    Task<IEnumerable<NotificationCollection>> GetSubscribersAsync(string intersection, string metric);
    Task<IEnumerable<NotificationCollection>> GetUserSubscriptionsAsync(string userId);
    Task DeactivateSubscriptionAsync(string userId, string userEmail, string intersection, string metric);
}