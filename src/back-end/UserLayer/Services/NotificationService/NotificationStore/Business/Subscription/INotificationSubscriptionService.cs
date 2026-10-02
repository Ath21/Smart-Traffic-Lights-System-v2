using System;
using NotificationData.Collections;
using NotificationStore.Models.Requests;
using NotificationStore.Models.Responses;

namespace NotificationStore.Business.Subscription;

public interface INotificationSubscriptionService
{
    Task<IEnumerable<SubscriptionResponse>> GetUserSubscriptionsAsync(string userId);
    Task UnsubscribeAsync(UnsubscribeRequest request);
}