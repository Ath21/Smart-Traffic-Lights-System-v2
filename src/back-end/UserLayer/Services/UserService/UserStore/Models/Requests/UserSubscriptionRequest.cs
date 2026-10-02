namespace UserStore.Models.Requests;

public class UserSubscriptionRequest
{
    public string Intersection { get; set; } = null!;
    public string Metric { get; set; } = null!;
}
