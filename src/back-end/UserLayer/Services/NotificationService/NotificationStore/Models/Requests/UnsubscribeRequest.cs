using System;

namespace NotificationStore.Models.Requests;

public class UnsubscribeRequest
{
    public string UserId { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string? Intersection { get; set; }
    public string? Metric { get; set; }
}
