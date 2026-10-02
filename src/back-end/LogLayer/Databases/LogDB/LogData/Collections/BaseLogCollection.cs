using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LogData.Collections;

public abstract class BaseLogCollection
{
    // ============================================================
    // Core Identification
    // ============================================================
    [BsonElement("correlation_id")]
    [BsonRepresentation(BsonType.String)]
    public Guid CorrelationId { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("source_layer")]
    public string? SourceLayer { get; set; } // User, Traffic, Sensor

    [BsonElement("source_level")]
    public string? SourceLevel { get; set; } // Edge, Cloud, Fog

    [BsonElement("source_service")]
    public string? SourceService { get; set; } // User Service, Traffic Light Controller Service

    [BsonElement("source_domain")]
    public string? SourceDomain { get; set; } // [CONSUMER][USER_REQUEST], [CONTROLLER][TRAFFIC_LIGHT]

    [BsonElement("type")]
    public string? Type { get; set; }   // audit | error | failover

    // ============================================================
    // Message Content
    // ============================================================
    [BsonElement("category")]
    public string? Category { get; set; } // API, CONSUMER, CONTROLLER, AUTH, DATA_PROCESSING

    [BsonElement("message")]
    public string? Message { get; set; } // Intersection Controller applied priority detection.

    [BsonElement("operation")]
    public string? Operation { get; set; } // e.g. "CreateUser", "ProcessSensorData"

    // ============================================================
    // Runtime & Context Info
    // ============================================================
    [BsonElement("hostname")]
    public string? Hostname { get; set; } // container name (Docker hostname)

    [BsonElement("container_ip")]
    public string? ContainerIp { get; set; } // dynamically resolved at startup

    [BsonElement("environment")]
    public string? Environment { get; set; } // e.g. "prod", "dev", "local"

    // ============================================================
    // Structured Data (optional, flexible JSON)
    // ============================================================
    [BsonElement("data")]
    public BsonDocument? Data { get; set; } // Flexible structured data
}
