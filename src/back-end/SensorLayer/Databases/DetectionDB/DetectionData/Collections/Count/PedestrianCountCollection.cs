using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DetectionData.Collections.Count;

[BsonDiscriminator("pedestrian_count")]
[BsonIgnoreExtraElements]
public class PedestrianCountCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PedestrianId { get; set; }

    [BsonElement("intersection_id")]
    public int IntersectionId { get; set; }

    [BsonElement("intersection")]
    public string? Intersection { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("count")]
    public int Count { get; set; }
}

