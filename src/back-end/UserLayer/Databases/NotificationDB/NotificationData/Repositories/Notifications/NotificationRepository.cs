using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using NotificationData.Collections;

namespace NotificationData.Repositories.Notifications;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationRepository> _logger;
    private const string domain = "[REPOSITORY][NOTIFICATION]";

    public NotificationRepository(NotificationDbContext context, ILogger<NotificationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateSubscriptionAsync(NotificationCollection subscription)
    {
        _logger.LogInformation("{Domain} Upserting subscription for {UserId}/{UserEmail} at {Intersection}/{Metric}\n",
            domain, subscription.UserId, subscription.UserEmail, subscription.Intersection, subscription.Metric);

        var filter = Builders<NotificationCollection>.Filter.And(
            Builders<NotificationCollection>.Filter.Eq(n => n.UserId, subscription.UserId),
            Builders<NotificationCollection>.Filter.Eq(n => n.UserEmail, subscription.UserEmail),
            Builders<NotificationCollection>.Filter.Eq(n => n.Intersection, subscription.Intersection),
            Builders<NotificationCollection>.Filter.Eq(n => n.Metric, subscription.Metric)
        );

        var update = Builders<NotificationCollection>.Update
            .Set(n => n.Active, true)
            .Set(n => n.SubscribedAt, DateTime.UtcNow);

        await _context.Notifications.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task<IEnumerable<NotificationCollection>> GetSubscribersAsync(string intersection, string metric)
    {
        _logger.LogInformation("{Domain} Fetching subscribers for {Intersection}/{Metric}\n", domain, intersection, metric);

        var filter = Builders<NotificationCollection>.Filter.And(
            Builders<NotificationCollection>.Filter.Eq(n => n.Intersection, intersection),
            Builders<NotificationCollection>.Filter.Eq(n => n.Metric, metric),
            Builders<NotificationCollection>.Filter.Eq(n => n.Active, true)
        );

        return await _context.Notifications.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<NotificationCollection>> GetUserSubscriptionsAsync(string userId)
    {
        _logger.LogInformation("{Domain} Fetching subscriptions for {UserId}\n", domain, userId);

        var filter = Builders<NotificationCollection>.Filter.And(
            Builders<NotificationCollection>.Filter.Eq(n => n.UserId, userId),
            Builders<NotificationCollection>.Filter.Eq(n => n.Active, true)
        );

        return await _context.Notifications.Find(filter).ToListAsync();
    }

    public async Task DeactivateSubscriptionAsync(string userId, string userEmail, string intersection, string metric)
    {
        _logger.LogInformation("{Domain} Deactivating subscription for {UserId} {UserEmail} {Intersection}/{Metric}\n",
            domain, userId, userEmail, intersection, metric);

        var filter = Builders<NotificationCollection>.Filter.And(
            Builders<NotificationCollection>.Filter.Eq(n => n.UserId, userId),
            Builders<NotificationCollection>.Filter.Eq(n => n.UserEmail, userEmail),
            Builders<NotificationCollection>.Filter.Eq(n => n.Intersection, intersection),
            Builders<NotificationCollection>.Filter.Eq(n => n.Metric, metric)
        );

        var update = Builders<NotificationCollection>.Update.Set(n => n.Active, false);
        await _context.Notifications.UpdateOneAsync(filter, update);
    }
}