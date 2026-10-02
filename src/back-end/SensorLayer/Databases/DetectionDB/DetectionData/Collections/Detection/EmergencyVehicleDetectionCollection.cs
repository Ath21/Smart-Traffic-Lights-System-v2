using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DetectionData.Collections.Detection;

[BsonDiscriminator("emergency_vehicle_detections")]
[BsonIgnoreExtraElements]
public class EmergencyVehicleDetectionCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? EmergencyId { get; set; } 

    [BsonElement("intersection_id")]
    public int IntersectionId { get; set; } 

    [BsonElement("intersection")]
    public string? Intersection { get; set; }

    [BsonElement("detectedAt")]
    public DateTime DetectedAt { get; set; }

    [BsonElement("direction")]
    public string? Direction { get; set; }

    public string? EmergencyVehicleType { get; set; }
      
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }
}

