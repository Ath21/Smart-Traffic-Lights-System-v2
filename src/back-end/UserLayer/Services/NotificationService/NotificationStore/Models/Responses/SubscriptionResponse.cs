using System;

namespace NotificationStore.Models.Responses;

public class SubscriptionResponse
{
    public string UserId { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string Intersection { get; set; } = null!;
    public string Metric { get; set; } = null!;
    public bool Active { get; set; }
    public DateTime SubscribedAt { get; set; }
}
