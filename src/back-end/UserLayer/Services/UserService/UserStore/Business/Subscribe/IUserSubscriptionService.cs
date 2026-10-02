using System;
using UserStore.Models.Requests;
using UserStore.Models.Responses;

namespace UserStore.Business.Subscribe;

public interface IUserSubscriptionService
{
    Task<SubscriptionResponse> SubscribeAsync(string userId, string email, UserSubscriptionRequest request);
}
