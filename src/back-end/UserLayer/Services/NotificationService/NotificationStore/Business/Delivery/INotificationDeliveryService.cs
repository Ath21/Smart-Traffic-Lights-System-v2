using System;
using NotificationStore.Models.Requests;
using NotificationStore.Models.Responses;

namespace NotificationStore.Business.Delivery;

public interface INotificationDeliveryService
{
    Task<IEnumerable<DeliveryResponse>> GetUserDeliveriesAsync(string userId, bool unreadOnly = false);
    Task MarkAsReadAsync(MarkAsReadRequest request);
}
