using System;

namespace NotificationStore.Models.Responses;

public class DeliveryResponse
{
    public string DeliveryId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string MessageId { get; set; } = null!;
    public DateTime DeliveredAt { get; set; }
    public string Status { get; set; } = "Success";
    public bool Read { get; set; }
}