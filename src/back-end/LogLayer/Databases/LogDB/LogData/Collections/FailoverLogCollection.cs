using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LogData.Collections;

public class FailoverLogCollection : BaseLogCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? FailoverId { get; set; }
}


