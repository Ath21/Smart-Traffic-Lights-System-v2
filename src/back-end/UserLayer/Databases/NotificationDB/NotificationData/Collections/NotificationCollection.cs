using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationData.Collections;

public class NotificationCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;
    
    [BsonElement("userEmail")]
    public string UserEmail { get; set; } = null!;

    [BsonElement("intersection")]
    public string Intersection { get; set; } = null!;

    [BsonElement("metric")]
    public string Metric { get; set; } = null!;

    [BsonElement("active")]
    public bool Active { get; set; } = true;

    [BsonElement("subscribedAt")]
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastNotifiedAt")]
    public DateTime? LastNotifiedAt { get; set; }
}
