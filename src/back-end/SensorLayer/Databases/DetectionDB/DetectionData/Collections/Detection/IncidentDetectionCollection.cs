using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DetectionData.Collections.Detection;

[BsonDiscriminator("incident_detections")]
[BsonIgnoreExtraElements]
public class IncidentDetectionCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? IncidentId { get; set; } 

    [BsonElement("intersection_id")]
    public int IntersectionId { get; set; } 

    [BsonElement("intersection")]
    public string? Intersection { get; set; }

    [BsonElement("reportedAt")]
    public DateTime ReportedAt { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }
    
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }
}


