using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Messages.User;
using UserStore.Publishers.Logs;

namespace UserStore.Publishers.Notifications;

public class UserNotificationPublisher : IUserNotificationPublisher
{
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<UserNotificationPublisher> _logger;
    private readonly IUserLogPublisher _logPublisher;
    private readonly string _routingKey;

    private const string Tag = "[PUBLISHER][USER_NOTIFICATION]";

    public UserNotificationPublisher(
        IPublishEndpoint publisher,
        IConfiguration configuration,
        ILogger<UserNotificationPublisher> logger,
        IUserLogPublisher logPublisher)
    {
        _publisher = publisher;
        _logger = logger;
        _logPublisher = logPublisher;

        _routingKey = configuration["RabbitMQ:RoutingKeys:User:NotificationRequests"]
                      ?? "user.notification.request";
    }

    public async Task PublishSubscriptionRequestAsync(UserNotificationRequest message)
    {
        try
        {
            await _publisher.Publish(message, ctx => ctx.SetRoutingKey(_routingKey));

            _logger.LogInformation(
                "{Tag} Published subscription request for {UserId} ({Email}) => {Intersection}/{Metric}",
                Tag, message.UserId, message.UserEmail, message.Intersection, message.Metric);

            await _logPublisher.PublishAuditAsync(
                domain: "[PUBLISHER][USER_NOTIFICATION]",
                messageText: $"{Tag} Subscription request sent to Notification Service successfully.",
                category: "SUBSCRIBE",
                data: new Dictionary<string, object>
                {
                    ["UserId"] = message.UserId,
                    ["Email"] = message.UserEmail,
                    ["Intersection"] = message.Intersection,
                    ["Metric"] = message.Metric
                },
                operation: "PublishSubscriptionRequestAsync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{Tag} Failed to publish subscription request for {UserId} ({Email}) → {Intersection}/{Metric}",
                Tag, message.UserId, message.UserEmail, message.Intersection, message.Metric);

            await _logPublisher.PublishErrorAsync(
                domain: "[PUBLISHER][USER_NOTIFICATION]",
                messageText: $"{Tag} Failed to publish subscription request: {ex.Message}",
                data: new Dictionary<string, object>
                {
                    ["UserId"] = message.UserId,
                    ["Email"] = message.UserEmail,
                    ["Intersection"] = message.Intersection,
                    ["Metric"] = message.Metric,
                    ["Exception"] = ex.Message,
                    ["StackTrace"] = ex.StackTrace ?? string.Empty
                },
                operation: "PublishSubscriptionRequestAsync");

            throw;
        }
    }
}
