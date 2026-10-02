using System;

namespace UserStore.Models.Responses;

public class UserAuditResponse
{
    public int AuditId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
