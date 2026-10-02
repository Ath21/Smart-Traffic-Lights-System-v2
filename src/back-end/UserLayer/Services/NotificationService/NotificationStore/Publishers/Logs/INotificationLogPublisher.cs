using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotificationStore.Publishers.Logs;

public interface INotificationLogPublisher
{
    Task PublishAuditAsync(string domain, string messageText, string? category = "system", Dictionary<string, object>? data = null, string? operation = null);
    Task PublishErrorAsync(string domain, string messageText, Dictionary<string, object>? data = null, string? operation = null);
}