using AutoMapper;
using DetectionCacheData.Repositories;
using DetectionData.Collections.Detection;
using DetectionData.Extensions;
using DetectionData.Repositories.EmergencyVehicle;
using DetectionData.Repositories.Incident;
using DetectionData.Repositories.PublicTransport;
using DetectionStore.Models.Responses;
using DetectionStore.Publishers.Event;
using DetectionStore.Publishers.Logs;
using Messages.Sensor.Detection;
using MongoDB.Bson;

namespace DetectionStore.Business;

public class DetectionBusiness : IDetectionBusiness
{
    private readonly IMapper _mapper;
    private readonly ILogger<DetectionBusiness> _logger;
    private readonly IEmergencyVehicleDetectionRepository _emergencyRepo;
    private readonly IPublicTransportDetectionRepository _publicRepo;
    private readonly IIncidentDetectionRepository _incidentRepo;
    private readonly IDetectionCacheRepository _cacheRepo;
    private readonly IDetectionEventPublisher _eventPublisher;
    private readonly IDetectionLogPublisher _logPublisher;

    private const string Domain = "[BUSINESS][DETECTION]";

    public DetectionBusiness(
        IMapper mapper,
        ILogger<DetectionBusiness> logger,
        IEmergencyVehicleDetectionRepository emergencyRepo,
        IPublicTransportDetectionRepository publicRepo,
        IIncidentDetectionRepository incidentRepo,
        IDetectionCacheRepository cacheRepo,
        IDetectionEventPublisher eventPublisher,
        IDetectionLogPublisher logPublisher)
    {
        _mapper = mapper;
        _logger = logger;
        _emergencyRepo = emergencyRepo;
        _publicRepo = publicRepo;
        _incidentRepo = incidentRepo;
        _cacheRepo = cacheRepo;
        _eventPublisher = eventPublisher;
        _logPublisher = logPublisher;
    }

    // ============================================================
    // EMERGENCY VEHICLE DETECTED
    // ============================================================
    public async Task ProcessEmergencyVehicleAsync(EmergencyVehicleDetectedMessage msg)
    {
        var document = new EmergencyVehicleDetectionCollection
        {
            IntersectionId = msg.IntersectionId,
            Intersection = msg.Intersection,
            Direction = msg.Direction,
            EmergencyVehicleType = msg.EmergencyVehicleType,
            DetectedAt = msg.DetectedAt,
            Metadata = BsonExtensions.ToBsonDocument(msg.Metadata)
        };

        await _emergencyRepo.InsertAsync(document);
        await _cacheRepo.SetEmergencyDetectedAsync(msg.IntersectionId, true);
        await _eventPublisher.PublishEmergencyVehicleDetectedAsync(msg);

        await _logPublisher.PublishAuditAsync(
            Domain,
            $"Emergency vehicle ({msg.EmergencyVehicleType}) detected at {msg.Intersection}",
            "system",
            new()
            {
                ["IntersectionId"] = msg.IntersectionId,
                ["IntersectionName"] = msg.Intersection,
                ["Direction"] = msg.Direction ?? "N/A",
                ["Type"] = msg.EmergencyVehicleType ?? "Unknown"
            },
            "ProcessEmergencyVehicleAsync"
        );

        _logger.LogInformation("{Domain}[EMERGENCY] Emergency vehicle persisted at {Intersection}\n", Domain, msg.Intersection);
    }

    // ============================================================
    // PUBLIC TRANSPORT DETECTED
    // ============================================================
    public async Task ProcessPublicTransportAsync(PublicTransportDetectedMessage msg)
    {
        var document = new PublicTransportDetectionCollection
        {
            IntersectionId = msg.IntersectionId,
            IntersectionName = msg.IntersectionName,
            LineName = msg.LineName,
            DetectedAt = msg.DetectedAt,
            Metadata = BsonExtensions.ToBsonDocument(msg.Metadata)
        };

        await _publicRepo.InsertAsync(document);
        await _cacheRepo.SetPublicTransportDetectedAsync(msg.IntersectionId, true);
        await _eventPublisher.PublishPublicTransportDetectedAsync(msg);

        await _logPublisher.PublishAuditAsync(
            Domain,
            $"Public transport (Line={msg.LineName}) detected at {msg.IntersectionName}",
            "system",
            new()
            {
                ["IntersectionId"] = msg.IntersectionId,
                ["IntersectionName"] = msg.IntersectionName,
                ["LineName"] = msg.LineName ?? "Unknown"
            },
            "ProcessPublicTransportAsync"
        );

        _logger.LogInformation("{Domain}[PUBLIC_TRANSPORT] Public transport persisted at {Intersection}\n", Domain, msg.IntersectionName);
    }

    // ============================================================
    // INCIDENT DETECTED
    // ============================================================
    public async Task ProcessIncidentAsync(IncidentDetectedMessage msg)
    {
        var document = new IncidentDetectionCollection
        {
            IntersectionId = msg.IntersectionId,
            Intersection = msg.Intersection,
            Description = msg.Description,
            ReportedAt = msg.ReportedAt,
            Metadata = BsonExtensions.ToBsonDocument(msg.Metadata)
        };

        await _incidentRepo.InsertAsync(document);
        await _cacheRepo.SetIncidentDetectedAsync(msg.IntersectionId, true);
        await _eventPublisher.PublishIncidentDetectedAsync(msg);

        await _logPublisher.PublishAuditAsync(
            Domain,
            $"Incident reported at {msg.Intersection}: {msg.Description}",
            "system",
            new()
            {
                ["IntersectionId"] = msg.IntersectionId,
                ["IntersectionName"] = msg.Intersection,
                ["Description"] = msg.Description ?? "N/A"
            },
            "ProcessIncidentAsync"
        );

        _logger.LogWarning("{Domain}[INCIDENT] Incident persisted at {Intersection}\n", Domain, msg.Intersection);
    }

    // ============================================================
    // QUERY METHODS
    // ============================================================
    public async Task<IEnumerable<EmergencyVehicleDetectionResponse>> GetRecentEmergenciesAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving recent emergencies for intersection {IntersectionId}\n", Domain, intersectionId);
        var data = await _emergencyRepo.GetRecentEmergenciesAsync(intersectionId);
        return _mapper.Map<IEnumerable<EmergencyVehicleDetectionResponse>>(data);
    }

    public async Task<IEnumerable<PublicTransportDetectionResponse>> GetRecentPublicTransportsAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving recent public transports for intersection {IntersectionId}\n", Domain, intersectionId);
        var data = await _publicRepo.GetRecentPublicTransportsAsync(intersectionId);
        return _mapper.Map<IEnumerable<PublicTransportDetectionResponse>>(data);
    }

    public async Task<IEnumerable<IncidentDetectionResponse>> GetRecentIncidentsAsync(int intersectionId)
    {
        _logger.LogInformation("{Domain} Retrieving recent incidents for intersection {IntersectionId}\n", Domain, intersectionId);
        var data = await _incidentRepo.GetRecentIncidentsAsync(intersectionId);
        return _mapper.Map<IEnumerable<IncidentDetectionResponse>>(data);
    }
}
