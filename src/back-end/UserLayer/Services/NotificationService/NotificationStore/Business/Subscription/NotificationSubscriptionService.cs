using NotificationData.Collections;
using NotificationData.Repositories.Notifications;
using NotificationStore.Business.Email;
using NotificationStore.Models.Requests;
using NotificationStore.Models.Responses;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Business.Subscription;

public class NotificationSubscriptionService : INotificationSubscriptionService
{
    private readonly INotificationRepository _repo;
    private readonly INotificationLogPublisher _logPublisher;
    private readonly IEmailService _emailService;

    private const string Domain = "[BUSINESS][SUBSCRIPTION]";

    public NotificationSubscriptionService(
        INotificationRepository repo,
        INotificationLogPublisher logPublisher,
        IEmailService emailService)
    {
        _repo = repo;
        _logPublisher = logPublisher;
        _emailService = emailService;
    }

    // ============================================================
    // GET USER SUBSCRIPTIONS
    // ============================================================
    public async Task<IEnumerable<SubscriptionResponse>> GetUserSubscriptionsAsync(string userId)
    {
        var subs = await _repo.GetUserSubscriptionsAsync(userId);

        await _logPublisher.PublishAuditAsync(
            domain: Domain,
            messageText: $"{Domain} Retrieved subscriptions for {userId}",
            category: "SUBSCRIPTION",
            operation: "GetUserSubscriptionsAsync",
            data: new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["Count"] = subs.Count()
            });

        return subs.Select(s => new SubscriptionResponse
        {
            UserId = s.UserId,
            UserEmail = s.UserEmail,
            Intersection = s.Intersection,
            Metric = s.Metric,
            Active = s.Active,
            SubscribedAt = s.SubscribedAt
        });
    }

    // ============================================================
    // UNSUBSCRIBE
    // ============================================================
    public async Task UnsubscribeAsync(UnsubscribeRequest request)
    {
        var affected = new List<(string Intersection, string Metric)>();

        if (request.Intersection == null || request.Metric == null)
        {
            var subs = await _repo.GetUserSubscriptionsAsync(request.UserId);
            foreach (var s in subs)
            {
                await _repo.DeactivateSubscriptionAsync(request.UserId, s.UserEmail, s.Intersection, s.Metric);
                affected.Add((s.Intersection, s.Metric));
            }
        }
        else
        {
            await _repo.DeactivateSubscriptionAsync(
                request.UserId, request.UserEmail, request.Intersection, request.Metric);
            affected.Add((request.Intersection, request.Metric));
        }

        await _logPublisher.PublishAuditAsync(
            domain: Domain,
            messageText: $"{Domain} {request.UserEmail} unsubscribed from {request.Intersection ?? "all"}/{request.Metric ?? "all"}",
            category: "SUBSCRIPTION",
            operation: "UnsubscribeAsync",
            data: new Dictionary<string, object>
            {
                ["UserId"] = request.UserId,
                ["Email"] = request.UserEmail,
                ["Intersection"] = request.Intersection ?? "all",
                ["Metric"] = request.Metric ?? "all"
            });

        // ============================================================
        // Send confirmation email
        // ============================================================
        try
        {
            var subject = $"Subscription Cancellation — UNIWA STLS";
            var body = $@"
            <html>
            <body style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;background-color:#f9fafb;padding:24px;color:#222;'>
                <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:8px;padding:24px;box-shadow:0 1px 3px rgba(0,0,0,0.1);'>
                <h2 style='color:#b91c1c;'>Subscription Cancellation Confirmed</h2>
                <p>Hello <strong>{request.UserEmail}</strong>,</p>
                <p>Your subscription to traffic updates has been successfully <b>cancelled</b>.</p>
                <p>Details:</p>
                <ul>
                    {string.Join("", affected.Select(a => $"<li>{a.Intersection} — {a.Metric}</li>"))}
                </ul>
                <p style='margin-top:16px;'>You can re-subscribe anytime through the UNIWA STLS dashboard.</p>
                <p style='margin-top:24px;font-size:13px;color:#6b7280;'>Thank you for using UNIWA STLS.<br/>- Notification Service</p>
                </div>
            </body>
            </html>";

            await _emailService.SendEmailAsync(request.UserEmail, subject, body);

            await _logPublisher.PublishAuditAsync(
                domain: Domain,
                messageText: $"{Domain} Unsubscription confirmation email sent to {request.UserEmail}",
                category: "EMAIL",
                operation: "SendEmail",
                data: new Dictionary<string, object>
                {
                    ["UserId"] = request.UserId,
                    ["Email"] = request.UserEmail
                });
        }
        catch (Exception ex)
        {
            await _logPublisher.PublishErrorAsync(
                domain: Domain,
                messageText: $"{Domain} Failed to send unsubscription email to {request.UserEmail}: {ex.Message}",
                operation: "SendEmail",
                data: new Dictionary<string, object>
                {
                    ["UserId"] = request.UserId,
                    ["Email"] = request.UserEmail,
                    ["Error"] = ex.Message
                });
        }
    }
}
