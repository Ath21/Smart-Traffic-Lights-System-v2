using System;

namespace NotificationStore.Models.Requests;

public class MarkAsReadRequest
{
    public string UserId { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string DeliveryId { get; set; } = null!;
}
