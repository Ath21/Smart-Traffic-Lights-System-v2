using System;

namespace LogData.Settings;

public class CollectionsSettings
{
    public string? AuditLogs { get; set; } // = "audit_logs";
    public string? ErrorLogs { get; set; } // = "error_logs";
    public string? FailoverLogs { get; set; } // = "failover_logs";
}
