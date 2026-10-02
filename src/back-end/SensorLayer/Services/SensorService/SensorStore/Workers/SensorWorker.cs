using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorStore.Domain;
using SensorStore.Business;
using SensorStore.Publishers.Count;
using SensorStore.Publishers.Logs;
using Messages.Sensor.Count;

namespace SensorStore.Workers;

public class SensorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensorWorker> _logger;
    private readonly IntersectionContext _intersection;
    private readonly Random _rand = new();

    public SensorWorker(IServiceScopeFactory scopeFactory, ILogger<SensorWorker> logger, IntersectionContext intersection)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intersection = intersection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[WORKER][SENSOR] Started for {Intersection} (Id={Id})", _intersection.Name, _intersection.Id);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var business = scope.ServiceProvider.GetRequiredService<ISensorBusiness>();
                var countPublisher = scope.ServiceProvider.GetRequiredService<ISensorCountPublisher>();
                var logPublisher = scope.ServiceProvider.GetRequiredService<ISensorLogPublisher>();

                var (vehicles, avgSpeed, avgWait, flowRate, breakdown, pedestrians, cyclists) = GenerateRealisticTrafficData();

                // VEHICLE
                var vMsg = new VehicleCountMessage
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    IntersectionId = _intersection.Id,
                    Intersection = _intersection.Name,
                    CountTotal = vehicles,
                    AverageSpeedKmh = avgSpeed,
                    AverageWaitTimeSec = avgWait,
                    FlowRate = flowRate,
                    VehicleBreakdown = breakdown
                };

                await countPublisher.PublishVehicleCountAsync(vMsg);
                await business.ProcessVehicleCountAsync(vMsg);
                await logPublisher.PublishAuditAsync(
                    "[WORKER][SENSOR]",
                    $"Vehicle count ({vehicles}) published for {_intersection.Name}",
                    "system",
                    new() { ["CountType"] = "Vehicle", ["IntersectionName"] = _intersection.Name },
                    "VehicleCountPublished");

                // PEDESTRIAN
                var pMsg = new PedestrianCountMessage
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    IntersectionId = _intersection.Id,
                    Intersection = _intersection.Name,
                    Count = pedestrians
                };

                await countPublisher.PublishPedestrianCountAsync(pMsg);
                await business.ProcessPedestrianCountAsync(pMsg);
                await logPublisher.PublishAuditAsync(
                    "[WORKER][SENSOR]",
                    $"Pedestrian count ({pedestrians}) published for {_intersection.Name}",
                    "system",
                    new() { ["CountType"] = "Pedestrian", ["IntersectionName"] = _intersection.Name },
                    "PedestrianCountPublished");

                // CYCLIST
                var cMsg = new CyclistCountMessage
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    IntersectionId = _intersection.Id,
                    Intersection = _intersection.Name,
                    Count = cyclists
                };

                await countPublisher.PublishCyclistCountAsync(cMsg);
                await business.ProcessCyclistCountAsync(cMsg);
                await logPublisher.PublishAuditAsync(
                    "[WORKER][SENSOR]",
                    $"Cyclist count ({cyclists}) published for {_intersection.Name}",
                    "system",
                    new() { ["CountType"] = "Cyclist", ["IntersectionName"] = _intersection.Name },
                    "CyclistCountPublished");

                var delay = DateTime.Now.Hour is >= 7 and < 9 or >= 17 and < 19 ? 10 : 25;
                await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WORKER][SENSOR] Loop error at {Intersection}", _intersection.Name);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("[WORKER][SENSOR] Stopped for {Intersection}", _intersection.Name);
    }

    private (int, double, double, double, Dictionary<string, int>, int, int) GenerateRealisticTrafficData()
    {
        var now = DateTime.Now;
        var hourFactor = now.Hour switch
        {
            >= 7 and < 9 => 1.4,
            >= 13 and < 15 => 1.2,
            >= 17 and < 19 => 1.5,
            _ => 0.7
        };

        double baseVeh = 40, basePed = 15, baseCyc = 5;
        switch (_intersection.Name)
        {
            case "Agiou Spyridonos": baseVeh = 70; basePed = 25; baseCyc = 5; break;
            case "Kentriki Pyli": baseVeh = 45; basePed = 40; baseCyc = 6; break;
            case "Anatoliki Pyli": baseVeh = 55; basePed = 20; baseCyc = 4; break;
            case "Dytiki Pyli": baseVeh = 30; basePed = 15; baseCyc = 3; break;
            case "Ekklisia / Edessis": baseVeh = 20; basePed = 10; baseCyc = 2; break;
        }

        var noiseVeh = _rand.NextDouble() * 0.25 + 0.85;
        var noisePed = _rand.NextDouble() * 0.3 + 0.85;
        var noiseCyc = _rand.NextDouble() * 0.4 + 0.8;

        var vehicleCount = (int)(baseVeh * hourFactor * noiseVeh);
        var pedestrianCount = (int)(basePed * hourFactor * noisePed);
        var cyclistCount = (int)(baseCyc * hourFactor * noiseCyc);
        var avgSpeed = Math.Round(25 + _rand.NextDouble() * 25, 1);
        var avgWait = Math.Round(Math.Max(5, 25 - avgSpeed / 2), 1);
        var flowRate = Math.Round(vehicleCount / (avgWait + 1), 2);

        var breakdown = new Dictionary<string, int>
        {
            ["car"] = (int)(vehicleCount * 0.75),
            ["bus"] = (int)(vehicleCount * 0.05),
            ["truck"] = (int)(vehicleCount * 0.05),
            ["motorcycle"] = (int)(vehicleCount * 0.15)
        };

        return (vehicleCount, avgSpeed, avgWait, flowRate, breakdown, pedestrianCount, cyclistCount);
    }
}
