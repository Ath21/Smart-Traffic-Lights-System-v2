using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NotificationData.Collections;
using NotificationData.Settings;

namespace NotificationData;

public class NotificationDbContext
{
    private readonly IMongoDatabase _database;

    public NotificationDbContext(IOptions<NotificationDbSettings> dbSettings)
    {
        var settings = dbSettings.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new ArgumentNullException(nameof(settings.ConnectionString), "MongoDB connection string is missing.");

        if (string.IsNullOrWhiteSpace(settings.Database))
            throw new ArgumentNullException(nameof(settings.Database), "MongoDB database name is missing.");

        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.Database);

        Notifications = _database.GetCollection<NotificationCollection>(
            settings.Collections.Notifications ?? "notifications"
        );

        DeliveryLogs = _database.GetCollection<DeliveryLogCollection>(
            settings.Collections.DeliveryLogs ?? "delivery_logs"
        );
    }

    public IMongoCollection<NotificationCollection> Notifications { get; }
    public IMongoCollection<DeliveryLogCollection> DeliveryLogs { get; }

    // Simple connectivity check (MongoDB ping)
    public async Task<bool> CanConnectAsync()
    {
        try
        {
            var command = new BsonDocument("ping", 1);
            await _database.RunCommandAsync<BsonDocument>(command);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MONGO][PING] Connection failed: {ex.Message}");
            return false;
        }
    }
}
