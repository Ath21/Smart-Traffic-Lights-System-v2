using AutoMapper;
using UserData.Entities;
using UserStore.Models.Requests;
using UserStore.Models.Responses;

namespace UserStore;

public class UserStoreProfile : Profile
{
    public UserStoreProfile()
    {
        // RegisterUserRequest → UserEntity
        CreateMap<RegisterUserRequest, UserEntity>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // handled by PasswordHasher
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Sessions, opt => opt.Ignore())
            .ForMember(dest => dest.Audits, opt => opt.Ignore());

        // UserEntity → UserResponse
        CreateMap<UserEntity, UserResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));

        // UserEntity → UserProfileResponse
        CreateMap<UserEntity, UserProfileResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()); // filled at runtime
    }
}
