using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LogData.Collections;

public class AuditLogCollection : BaseLogCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AuditId { get; set; }
}
