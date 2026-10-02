using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DetectionStore.Domain;
using DetectionStore.Business;
using DetectionStore.Publishers.Event;
using DetectionStore.Publishers.Logs;
using Messages.Sensor.Detection;

namespace DetectionStore.Workers;

public class DetectionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DetectionWorker> _logger;
    private readonly IntersectionContext _intersection;
    private readonly Random _rand = new();
    private const string domain = "[WORKER][DETECTION]";

    public DetectionWorker(IServiceScopeFactory scopeFactory, ILogger<DetectionWorker> logger, IntersectionContext intersection)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intersection = intersection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Domain} Started for {Intersection} (Id={Id})\n", domain, _intersection.Name, _intersection.Id);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IDetectionEventPublisher>();
                var logPublisher = scope.ServiceProvider.GetRequiredService<IDetectionLogPublisher>();
                var business = scope.ServiceProvider.GetRequiredService<IDetectionBusiness>();

                if (_rand.NextDouble() < 0.10)
                {
                    var msg = GenerateEmergencyVehicleMessage();
                    await eventPublisher.PublishEmergencyVehicleDetectedAsync(msg);
                    await business.ProcessEmergencyVehicleAsync(msg);

                    await logPublisher.PublishAuditAsync(
                        "[WORKER][DETECTION]",
                        $"Emergency vehicle ({msg.EmergencyVehicleType}) detected at {_intersection.Name}",
                        "system",
                        new() { ["EventType"] = "EmergencyVehicle", ["IntersectionName"] = _intersection.Name, ["Direction"] = msg.Direction ?? "unknown" },
                        "EmergencyVehicleDetected");
                }

                if (_rand.NextDouble() < 0.30)
                {
                    var msg = GeneratePublicTransportMessage();
                    await eventPublisher.PublishPublicTransportDetectedAsync(msg);
                    await business.ProcessPublicTransportAsync(msg);

                    await logPublisher.PublishAuditAsync(
                        "[WORKER][DETECTION]",
                        $"Public transport ({msg.LineName}) detected at {_intersection.Name}",
                        "system",
                        new() { ["EventType"] = "PublicTransport", ["LineName"] = msg.LineName ?? "unknown", ["IntersectionName"] = _intersection.Name },
                        "PublicTransportDetected");
                }

                if (_rand.NextDouble() < 0.05)
                {
                    var msg = GenerateIncidentMessage();
                    await eventPublisher.PublishIncidentDetectedAsync(msg);
                    await business.ProcessIncidentAsync(msg);

                    await logPublisher.PublishFailoverAsync(
                        "[WORKER][DETECTION]",
                        $"Incident detected at {_intersection.Name}: {msg.Description}",
                        new() { ["EventType"] = "Incident", ["IntersectionName"] = _intersection.Name, ["Description"] = msg.Description ?? "N/A" },
                        "IncidentDetected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Domain} Loop error at {Intersection}\n", domain, _intersection.Name);
            }

            await Task.Delay(TimeSpan.FromSeconds(_rand.Next(25, 40)), stoppingToken);
        }

        _logger.LogInformation("{Domain} Stopped for {Intersection}\n", domain, _intersection.Name);
    }

    private EmergencyVehicleDetectedMessage GenerateEmergencyVehicleMessage()
    {
        var type = new[] { "Ambulance", "Firetruck", "Police Car" }[_rand.Next(3)];
        return new()
        {
            CorrelationId = Guid.NewGuid().ToString(),
            IntersectionId = _intersection.Id,
            Intersection = _intersection.Name,
            DetectedAt = DateTime.UtcNow,
            Direction = GetRandomDirection(),
            EmergencyVehicleType = type,
            Metadata = new Dictionary<string, object>
            {
                ["SpeedKmh"] = _rand.Next(40, 100),
                ["SignalPriorityLevel"] = type == "Ambulance" ? "High" : type == "Firetruck" ? "Medium" : "Law Enforcement",
                ["SirensDetected"] = _rand.NextDouble() > 0.2,
                ["EstimatedArrivalSec"] = _rand.Next(3, 12),
                ["Coordinates"] = GetRandomCoordinates()
            }
        };
    }

    private PublicTransportDetectedMessage GeneratePublicTransportMessage()
    {
        var line = $"Bus{_rand.Next(700, 899)}";
        return new()
        {
            CorrelationId = Guid.NewGuid().ToString(),
            IntersectionId = _intersection.Id,
            IntersectionName = _intersection.Name,
            DetectedAt = DateTime.UtcNow,
            LineName = line,
            Metadata = new Dictionary<string, object>
            {
                ["SpeedKmh"] = _rand.Next(20, 60),
                ["OccupancyRatio"] = Math.Round(_rand.NextDouble(), 2),
                ["ScheduleDeviationMin"] = _rand.Next(-3, 5),
                ["PriorityReason"] = _rand.NextDouble() < 0.4 ? "Running Late" : "Schedule Priority"
            }
        };
    }

    private IncidentDetectedMessage GenerateIncidentMessage()
    {
        var type = new[] { "collision", "breakdown", "roadwork", "obstruction" }[_rand.Next(4)];
        return new()
        {
            CorrelationId = Guid.NewGuid().ToString(),
            IntersectionId = _intersection.Id,
            Intersection = _intersection.Name,
            ReportedAt = DateTime.UtcNow,
            Description = $"Incident: {type}",
            Metadata = new Dictionary<string, object>
            {
                ["IncidentType"] = type,
                ["SeverityLevel"] = _rand.Next(1, 5),
                ["LanesBlocked"] = _rand.Next(0, 2),
                ["EstimatedDurationMin"] = _rand.Next(5, 25)
            }
        };
    }

    private static string GetRandomDirection() => new[] { "north", "south", "east", "west" }[Random.Shared.Next(4)];

    private string GetRandomCoordinates()
    {
        var baseLat = 37.9750;
        var baseLon = 23.7350;
        var lat = baseLat + (_rand.NextDouble() - 0.5) * 0.002;
        var lon = baseLon + (_rand.NextDouble() - 0.5) * 0.002;
        return $"{lat:F6}, {lon:F6}";
    }
}
