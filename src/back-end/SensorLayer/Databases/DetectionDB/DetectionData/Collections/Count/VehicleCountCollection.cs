using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DetectionData.Collections.Count;

[BsonDiscriminator("vehicle_count")]
[BsonIgnoreExtraElements]
public class VehicleCountCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? VehicleId { get; set; } 

    [BsonElement("intersection_id")]
    public int IntersectionId { get; set; } 

    [BsonElement("intersection")]
    public string? Intersection { get; set; } 

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("count_total")]
    public int CountTotal { get; set; } 

    [BsonElement("average_speed_kmh")]
    public double AverageSpeedKmh { get; set; }

    [BsonElement("average_wait_time_sec")]
    public double AverageWaitTimeSec { get; set; }

    [BsonElement("flow_rate")]
    public double? FlowRate { get; set; } // vehicles per minute

    [BsonElement("vehicle_breakdown")]
    public Dictionary<string, int>? VehicleBreakdown { get; set; } // e.g., car, truck, bus, motorcycle   
}



