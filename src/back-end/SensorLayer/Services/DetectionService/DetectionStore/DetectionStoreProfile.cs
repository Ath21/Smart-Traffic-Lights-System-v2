using AutoMapper;
using DetectionData.Collections.Detection;
using DetectionStore.Models.Requests;
using DetectionStore.Models.Responses;

namespace DetectionStore;

public class DetectionStoreProfile : Profile
{
    public DetectionStoreProfile()
    {
        // EMERGENCY VEHICLE
        CreateMap<EmergencyVehicleDetectionRequest, EmergencyVehicleDetectionCollection>()
            .ForMember(dest => dest.DetectedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<EmergencyVehicleDetectionCollection, EmergencyVehicleDetectionResponse>();

        // PUBLIC TRANSPORT
        CreateMap<PublicTransportDetectionRequest, PublicTransportDetectionCollection>()
            .ForMember(dest => dest.DetectedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<PublicTransportDetectionCollection, PublicTransportDetectionResponse>();

        // INCIDENT
        CreateMap<IncidentDetectionRequest, IncidentDetectionCollection>()
            .ForMember(dest => dest.ReportedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<IncidentDetectionCollection, IncidentDetectionResponse>();
    }
}
