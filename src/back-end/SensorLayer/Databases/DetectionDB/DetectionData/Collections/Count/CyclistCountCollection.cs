using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DetectionData.Collections.Count;

[BsonDiscriminator("cyclist_count")]
[BsonIgnoreExtraElements]
public class CyclistCountCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CyclistId { get; set; } 

    [BsonElement("intersection_id")]
    public int IntersectionId { get; set; }

    [BsonElement("intersection")]
    public string? Intersection { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } 

    [BsonElement("count")]
    public int Count { get; set; }
}

