using System;

namespace UserStore.Models.Responses;


public class SubscriptionResponse
{
    public string UserId { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string Intersection { get; set; } = null!;
    public string Metric { get; set; } = null!;
    public string Status { get; set; } = "Published";
}
