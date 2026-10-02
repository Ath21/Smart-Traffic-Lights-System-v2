using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LogData.Collections;

public class ErrorLogCollection : BaseLogCollection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ErrorId { get; set; }
}

