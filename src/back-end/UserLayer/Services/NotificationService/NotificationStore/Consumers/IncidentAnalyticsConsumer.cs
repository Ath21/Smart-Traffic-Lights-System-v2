using MassTransit;
using Messages.Traffic.Analytics;
using NotificationData.Repositories.DeliveryLogs;
using NotificationData.Repositories.Notifications;
using NotificationStore.Business.Email;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Consumers;

public class IncidentAnalyticsConsumer : IConsumer<IncidentAnalyticsMessage>
{
    private readonly INotificationRepository _repo;
    private readonly IDeliveryLogRepository _logRepo;
    private readonly INotificationLogPublisher _logPublisher;
    private readonly IEmailService _emailService;

    private const string Domain = "[CONSUMER][ANALYTICS][INCIDENT]";

    public IncidentAnalyticsConsumer(
        INotificationRepository repo,
        IDeliveryLogRepository logRepo,
        INotificationLogPublisher logPublisher,
        IEmailService emailService)
    {
        _repo = repo;
        _logRepo = logRepo;
        _logPublisher = logPublisher;
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<IncidentAnalyticsMessage> context)
    {
        var msg = context.Message;

        await _logPublisher.PublishAuditAsync(
            domain: Domain,
            messageText: $"{Domain} Received {msg.IncidentType} ({msg.Severity}) at {msg.Intersection}",
            category: "ANALYTICS",
            operation: "Consume",
            data: new Dictionary<string, object>
            {
                ["Intersection"] = msg.Intersection,
                ["IncidentType"] = msg.IncidentType,
                ["Severity"] = msg.Severity.ToString()
            });

        var subs = await _repo.GetSubscribersAsync(msg.Intersection, "Incidents");

        foreach (var sub in subs)
        {
            var subject = $"Incident at {msg.Intersection}";
            var body = $@"
            <html>
            <body style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;background-color:#fff8f8;padding:24px;'>
                <div style='max-width:600px;margin:auto;background:#ffffff;border:1px solid #fca5a5;border-radius:8px;padding:24px;'>
                <h2 style='color:#b91c1c;'>Traffic Incident Alert</h2>
                <p>An incident was reported at <strong>{msg.Intersection}</strong>.</p>
                <p><strong>Type:</strong> {msg.IncidentType}</p>
                <p><strong>Severity:</strong> {msg.Severity}</p>
                <p><strong>Time:</strong> {msg.Timestamp:u}</p>
                <p>Please exercise caution and follow local instructions.</p>
                <p style='margin-top:24px;font-size:13px;color:#6b7280;'>UNIWA STLS Notification Service</p>
                </div>
            </body>
            </html>";

            try
            {
                await _emailService.SendEmailAsync(sub.UserEmail, subject, body);
                await _logRepo.LogDeliveryAsync(sub.UserId, sub.UserEmail, $"{msg.Intersection}.incident", "Success");

                await _logPublisher.PublishAuditAsync(
                    domain: Domain,
                    messageText: $"{Domain} Email sent to {sub.UserEmail}",
                    category: "EMAIL",
                    operation: "SendEmail",
                    data: new Dictionary<string, object>
                    {
                        ["UserEmail"] = sub.UserEmail,
                        ["Intersection"] = msg.Intersection
                    });
            }
            catch (Exception ex)
            {
                await _logRepo.LogDeliveryAsync(sub.UserId, sub.UserEmail, $"{msg.Intersection}.incident", "Failed");

                await _logPublisher.PublishErrorAsync(
                    domain: Domain,
                    messageText: $"{Domain} Failed to send email to {sub.UserEmail}: {ex.Message}",
                    operation: "SendEmail",
                    data: new Dictionary<string, object>
                    {
                        ["UserEmail"] = sub.UserEmail,
                        ["Intersection"] = msg.Intersection,
                        ["Error"] = ex.Message
                    });
            }
        }
    }
}
