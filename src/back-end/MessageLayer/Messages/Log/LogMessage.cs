namespace Messages.Log;

public class LogMessage
{
    // ============================================================
    // Core Identification
    // ============================================================
    public string Layer { get; set; } = null!;            // "User", "Traffic", "Sensor"
    public string Level { get; set; } = null!;            // "Edge", "Fog", "Cloud"
    public string Service { get; set; } = null!;          // "UserStore", "TrafficLightController"
    public string Domain { get; set; } = null!;           // "[CONSUMER][USER_REQUEST]"
    public string Type { get; set; } = "audit";           // audit | error | failover
    public string Category { get; set; } = "system";      // API, CONSUMER, CONTROLLER, AUTH, DATA_PROCESSING
    public string Message { get; set; } = null!;
    public string? Operation { get; set; }                // e.g. "CreateUser", "ProcessSensorData"

    // ============================================================
    // Correlation & Context
    // ============================================================
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ============================================================
    // Runtime Information
    // ============================================================
    public string? Hostname { get; set; }                 // container name
    public string? ContainerIp { get; set; }              // container IP
    public string? Environment { get; set; }              // prod | dev | local

    // ============================================================
    // Structured Data
    // ============================================================
    public Dictionary<string, object>? Data { get; set; }
}
