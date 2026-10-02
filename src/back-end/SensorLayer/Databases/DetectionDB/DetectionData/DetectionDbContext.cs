using DetectionData.Collections;
using DetectionData.Collections.Count;
using DetectionData.Collections.Detection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DetectionData;

public class DetectionDbContext
{
    private readonly IMongoDatabase _database;

    public DetectionDbContext(IOptions<DetectionDbSettings> detectionDbSettings)
    {
        var mongoClient = new MongoClient(detectionDbSettings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(detectionDbSettings.Value.Database);

        VehicleCount = _database.GetCollection<VehicleCountCollection>(detectionDbSettings.Value.Collections.VehicleCount);
        PedestrianCount = _database.GetCollection<PedestrianCountCollection>(detectionDbSettings.Value.Collections.PedestrianCount);
        CyclistCount = _database.GetCollection<CyclistCountCollection>(detectionDbSettings.Value.Collections.CyclistCount);
        PublicTransportDetections = _database.GetCollection<PublicTransportDetectionCollection>(detectionDbSettings.Value.Collections.PublicTransport);
        EmergencyVehicleDetections = _database.GetCollection<EmergencyVehicleDetectionCollection>(detectionDbSettings.Value.Collections.EmergencyVehicle);
        IncidentDetections = _database.GetCollection<IncidentDetectionCollection>(detectionDbSettings.Value.Collections.Incident);
    }

    // ===============================
    // Mongo Collections
    // ===============================
    public IMongoCollection<VehicleCountCollection> VehicleCount { get; }
    public IMongoCollection<PedestrianCountCollection> PedestrianCount { get; }
    public IMongoCollection<CyclistCountCollection> CyclistCount { get; }
    public IMongoCollection<PublicTransportDetectionCollection> PublicTransportDetections { get; }
    public IMongoCollection<EmergencyVehicleDetectionCollection> EmergencyVehicleDetections { get; }
    public IMongoCollection<IncidentDetectionCollection> IncidentDetections { get; }

    // ===============================
    // Ping Check
    // ===============================
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
