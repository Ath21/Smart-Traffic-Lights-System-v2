using LogData.Collections;
using LogData.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LogData;

public class LogDbContext
{
    private readonly IMongoDatabase _database;

    public LogDbContext(IOptions<LogDbSettings> logDbSettings)
    {
        var mongoClient = new MongoClient(logDbSettings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(logDbSettings.Value.Database);

        AuditLogs = _database.GetCollection<AuditLogCollection>(logDbSettings.Value.Collections.AuditLogs);
        ErrorLogs = _database.GetCollection<ErrorLogCollection>(logDbSettings.Value.Collections.ErrorLogs);
        FailoverLogs = _database.GetCollection<FailoverLogCollection>(logDbSettings.Value.Collections.FailoverLogs);
    }

    public IMongoCollection<AuditLogCollection> AuditLogs { get; }
    public IMongoCollection<ErrorLogCollection> ErrorLogs { get; }
    public IMongoCollection<FailoverLogCollection> FailoverLogs { get; }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            var command = new BsonDocument("ping", 1);
            await _database.RunCommandAsync<BsonDocument>(command);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
