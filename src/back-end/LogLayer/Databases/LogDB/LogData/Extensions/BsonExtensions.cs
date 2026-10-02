using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text.Json;

namespace LogData.Extensions;

public static class BsonExtensions
{
    // ============================================================
    // Convert BsonDocument → Dictionary<string, object>
    // ============================================================
    public static Dictionary<string, object> ToDictionary(this BsonDocument bson)
    {
        var dict = new Dictionary<string, object>();

        foreach (var element in bson.Elements)
        {
            if (element.Value == null || element.Value.IsBsonNull)
            {
                dict[element.Name] = string.Empty;
                continue;
            }

            switch (element.Value.BsonType)
            {
                case BsonType.Document:
                case BsonType.Array:
                    // Serialize nested doc or array as JSON string
                    dict[element.Name] = element.Value.ToJson();
                    break;

                case BsonType.Boolean:
                    dict[element.Name] = element.Value.AsBoolean;
                    break;

                case BsonType.Int32:
                    dict[element.Name] = element.Value.AsInt32;
                    break;

                case BsonType.Int64:
                    dict[element.Name] = element.Value.AsInt64;
                    break;

                case BsonType.Double:
                    dict[element.Name] = element.Value.AsDouble;
                    break;

                case BsonType.DateTime:
                    dict[element.Name] = element.Value.ToUniversalTime();
                    break;

                default:
                    dict[element.Name] = element.Value.ToString() ?? string.Empty;
                    break;
            }
        }

        return dict;
    }

    // ============================================================
    // Convert Dictionary<string, object> → BsonDocument
    // ============================================================
    public static BsonDocument ToBsonDocument(this Dictionary<string, object> dict)
    {
        var bson = new BsonDocument();

        foreach (var (key, value) in dict)
        {
            if (value == null)
            {
                bson[key] = BsonNull.Value;
                continue;
            }

            switch (value)
            {
                case string s when string.IsNullOrWhiteSpace(s):
                    bson[key] = BsonNull.Value;
                    break;

                case string s:
                    // Try parsing JSON-like string values into BSON
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(s);
                        bson[key] = jsonDoc.RootElement.ValueKind switch
                        {
                            JsonValueKind.Object => BsonDocument.Parse(s),
                            JsonValueKind.Array => BsonSerializer.Deserialize<BsonArray>(s),
                            JsonValueKind.String => new BsonString(jsonDoc.RootElement.GetString()),
                            JsonValueKind.Number => jsonDoc.RootElement.TryGetInt64(out var l)
                                ? new BsonInt64(l)
                                : new BsonDouble(jsonDoc.RootElement.GetDouble()),
                            JsonValueKind.True => BsonBoolean.True,
                            JsonValueKind.False => BsonBoolean.False,
                            _ => BsonValue.Create(s)
                        };
                    }
                    catch
                    {
                        bson[key] = BsonValue.Create(s);
                    }
                    break;

                case int i:
                    bson[key] = new BsonInt32(i);
                    break;

                case long l:
                    bson[key] = new BsonInt64(l);
                    break;

                case double d:
                    bson[key] = new BsonDouble(d);
                    break;

                case bool b:
                    bson[key] = new BsonBoolean(b);
                    break;

                case DateTime dt:
                    bson[key] = new BsonDateTime(dt.ToUniversalTime());
                    break;

                default:
                    bson[key] = BsonValue.Create(value.ToString());
                    break;
            }
        }

        return bson;
    }
}
