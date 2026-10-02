using System;

namespace TrafficLightControllerStore.Publishers.Logs;

public interface ITrafficLightLogPublisher 
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