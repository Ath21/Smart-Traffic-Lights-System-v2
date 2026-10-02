using System;
using System.Collections.Generic;

namespace LogStore.Models.Responses;

public class SearchLogResponse
{
    // ============================================================
    // Classification
    // ============================================================
    public string LogType { get; set; } = string.Empty;   // "Audit" | "Error" | "Failover"

    // ============================================================
    // Core Identification
    // ============================================================
    public Guid CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }

    // ============================================================
    // Source Hierarchy
    // ============================================================
    public string Layer { get; set; } = string.Empty;         // e.g. "Traffic", "User", "Notification"
    public string Level { get; set; } = string.Empty;         // e.g. "Edge", "Fog", "Cloud"
    public string Service { get; set; } = string.Empty;       // e.g. "TrafficLightController"
    public string Domain { get; set; } = string.Empty;        // e.g. "[CONSUMER][USER_REQUEST]"

    // ============================================================
    // Log Content
    // ============================================================
    public string Type { get; set; } = string.Empty;          // audit | error | failover
    public string Category { get; set; } = string.Empty;      // API | Controller | Consumer | Auth | DataProcessing
    public string? Operation { get; set; }                    // e.g. "ProcessSensorData"
    public string? Message { get; set; }                      // Main log message

    // ============================================================
    // Context & Runtime
    // ============================================================
    public string? EntityId { get; set; }                     // userId | intersectionId | containerIp
    public string? Hostname { get; set; }                     // Docker container name
    public string? ContainerIp { get; set; }                  // Container IP
    public string? Environment { get; set; }                  // prod | dev | local

    // ============================================================
    // Flexible Data Payload
    // ============================================================
    public Dictionary<string, object>? Data { get; set; }     // Structured, service-specific info (intersection, counts, etc.)
}
