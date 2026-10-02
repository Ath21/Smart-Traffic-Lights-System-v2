using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DetectionData.Collections.Detection;

[BsonDiscriminator("public_transport_detections")]
[BsonIgnoreExtraElements]
public class PublicTransportDetectionCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PublicId { get; set; } 

    [BsonElement("intersection_id")]
    public int IntersectionId { get; set; }

    [BsonElement("intersection_name")]
    public string? IntersectionName { get; set; }

    [BsonElement("line_name")]
    public string? LineName { get; set; } 

    [BsonElement("detectedAt")]
    public DateTime DetectedAt { get; set; } 
    
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }
}


