using Messages.Log;

namespace TrafficAnalytics.Publishers.Logs;

public interface IAnalyticsLogPublisher
{
    Task PublishAuditAsync(
        string domain,
        string messageText,
        string? category = "system",
        Dictionary<string, object>? data = null,
        string? operation = null);

    Task PublishErrorAsync(
        string domain,
        string messageText,
        Dictionary<string, object>? data = null,
        string? operation = null);
}
