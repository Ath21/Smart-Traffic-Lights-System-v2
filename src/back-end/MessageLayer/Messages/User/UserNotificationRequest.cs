using System;

namespace Messages.User;

public class UserNotificationRequest
{
    public string UserId { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string Intersection { get; set; } = null!;
    public string Metric { get; set; } = null!;
}
