using Messages.User;

namespace UserStore.Publishers.Notifications;

public interface IUserNotificationPublisher
{
    Task PublishSubscriptionRequestAsync(UserNotificationRequest message);
}

