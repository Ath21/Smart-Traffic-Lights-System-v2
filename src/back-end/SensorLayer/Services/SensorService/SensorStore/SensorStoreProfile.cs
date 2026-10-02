using AutoMapper;
using DetectionData.Collections.Count;
using SensorStore.Models.Requests;
using SensorStore.Models.Responses;

namespace SensorStore;

public class SensorStoreProfile : Profile
{
    public SensorStoreProfile()
    {
        // Requests → Mongo collections
        CreateMap<VehicleCountRequest, VehicleCountCollection>()
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<PedestrianCountRequest, PedestrianCountCollection>()
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<CyclistCountRequest, CyclistCountCollection>()
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // Collections → Responses
        CreateMap<VehicleCountCollection, VehicleCountResponse>();
        CreateMap<PedestrianCountCollection, PedestrianCountResponse>();
        CreateMap<CyclistCountCollection, CyclistCountResponse>();
    }
}