using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationData.Collections;

public class DeliveryLogCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;
    [BsonElement("userEmail")]
    public string UserEmail { get; set; } = null!;

    [BsonElement("messageId")]
    public string MessageId { get; set; } = null!;

    [BsonElement("deliveredAt")]
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public string Status { get; set; } = "Success";

    [BsonElement("retryCount")]
    public int RetryCount { get; set; } = 0;
}
