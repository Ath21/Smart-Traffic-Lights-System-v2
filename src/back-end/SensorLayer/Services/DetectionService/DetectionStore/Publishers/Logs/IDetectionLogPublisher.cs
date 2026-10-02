using Messages.Log;

namespace DetectionStore.Publishers.Logs;

public interface IDetectionLogPublisher
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

    Task PublishFailoverAsync(
        string domain,
        string messageText,
        Dictionary<string, object>? data = null,
        string? operation = null);
}
