using AutoMapper;
using DetectionCacheData.Repositories;
using DetectionData.Collections.Count;
using DetectionData.Repositories.Vehicle;
using DetectionData.Repositories.Pedestrian;
using DetectionData.Repositories.Cyclist;
using Messages.Sensor.Count;
using SensorStore.Models.Responses;

namespace SensorStore.Business;

public class SensorBusiness : ISensorBusiness
{
    private readonly IVehicleCountRepository _vehicleRepo;
    private readonly IPedestrianCountRepository _pedestrianRepo;
    private readonly ICyclistCountRepository _cyclistRepo;
    private readonly IDetectionCacheRepository _cacheRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<SensorBusiness> _logger;
    private const string domain = "[BUSINESS][SENSOR]";

    public SensorBusiness(
        IVehicleCountRepository vehicleRepo,
        IPedestrianCountRepository pedestrianRepo,
        ICyclistCountRepository cyclistRepo,
        IDetectionCacheRepository cacheRepo,
        IMapper mapper,
        ILogger<SensorBusiness> logger)
    {
        _vehicleRepo = vehicleRepo;
        _pedestrianRepo = pedestrianRepo;
        _cyclistRepo = cyclistRepo;
        _cacheRepo = cacheRepo;
        _mapper = mapper;
        _logger = logger;
    }

    // ============================================================
    // VEHICLE COUNT
    // ============================================================
    public async Task ProcessVehicleCountAsync(VehicleCountMessage msg)
    {
        var document = new VehicleCountCollection
        {
            IntersectionId = msg.IntersectionId,
            Intersection = msg.Intersection,
            Timestamp = msg.Timestamp,
            CountTotal = msg.CountTotal,
            AverageSpeedKmh = msg.AverageSpeedKmh,
            AverageWaitTimeSec = msg.AverageWaitTimeSec,
            FlowRate = msg.FlowRate,
            VehicleBreakdown = msg.VehicleBreakdown
        };

        await _vehicleRepo.InsertAsync(document);
        await _cacheRepo.SetVehicleCountAsync(msg.IntersectionId, msg.CountTotal);

        _logger.LogInformation(
            "{Domain} Vehicle count persisted for {Intersection} | Total={Total}, FlowRate={FlowRate:F2}\n",
            domain, msg.Intersection, msg.CountTotal, msg.FlowRate);
    }

    // ============================================================
    // PEDESTRIAN COUNT
    // ============================================================
    public async Task ProcessPedestrianCountAsync(PedestrianCountMessage msg)
    {
        var document = new PedestrianCountCollection
        {
            IntersectionId = msg.IntersectionId,
            Intersection = msg.Intersection,
            Timestamp = msg.Timestamp,
            Count = msg.Count
        };

        await _pedestrianRepo.InsertAsync(document);
        await _cacheRepo.SetPedestrianCountAsync(msg.IntersectionId, msg.Count);

        _logger.LogInformation(
            "{Domain} Pedestrian count persisted for {Intersection} | Count={Count}\n",
            domain, msg.Intersection, msg.Count);
    }

    // ============================================================
    // CYCLIST COUNT
    // ============================================================
    public async Task ProcessCyclistCountAsync(CyclistCountMessage msg)
    {
        var document = new CyclistCountCollection
        {
            IntersectionId = msg.IntersectionId,
            Intersection = msg.Intersection,
            Timestamp = msg.Timestamp,
            Count = msg.Count
        };

        await _cyclistRepo.InsertAsync(document);
        await _cacheRepo.SetCyclistCountAsync(msg.IntersectionId, msg.Count);

        _logger.LogInformation(
            "{Domain} Cyclist count persisted for {Intersection} | Count={Count}\n",
            domain, msg.Intersection, msg.Count);
    }

    // ============================================================
    // VEHICLE COUNT HISTORY
    // ============================================================
    public async Task<IEnumerable<VehicleCountResponse>> GetRecentVehicleCountsAsync(int intersectionId)
    {
        var data = await _vehicleRepo.GetRecentByIntersectionAsync(intersectionId);
        var mapped = _mapper.Map<IEnumerable<VehicleCountResponse>>(data);

        _logger.LogInformation(
            "{Domain} Retrieved {Count} vehicle count records for intersection {Id}\n",
            domain, mapped.Count(), intersectionId);

        return mapped;
    }

    // ============================================================
    // PEDESTRIAN COUNT HISTORY
    // ============================================================
    public async Task<IEnumerable<PedestrianCountResponse>> GetRecentPedestrianCountsAsync(int intersectionId)
    {
        var data = await _pedestrianRepo.GetRecentByIntersectionAsync(intersectionId);
        var mapped = _mapper.Map<IEnumerable<PedestrianCountResponse>>(data);

        _logger.LogInformation(
            "{Domain} Retrieved {Count} pedestrian count records for intersection {Id}\n",
            domain, mapped.Count(), intersectionId);

        return mapped;
    }

    // ============================================================
    // CYCLIST COUNT HISTORY
    // ============================================================
    public async Task<IEnumerable<CyclistCountResponse>> GetRecentCyclistCountsAsync(int intersectionId)
    {
        var data = await _cyclistRepo.GetRecentByIntersectionAsync(intersectionId);
        var mapped = _mapper.Map<IEnumerable<CyclistCountResponse>>(data);

        _logger.LogInformation(
            "{Domain} Retrieved {Count} cyclist count records for intersection {Id}\n",
            domain, mapped.Count(), intersectionId);

        return mapped;
    }
}
