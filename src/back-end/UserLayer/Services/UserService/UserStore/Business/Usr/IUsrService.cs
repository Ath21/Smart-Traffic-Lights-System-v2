using UserStore.Models.Requests;
using UserStore.Models.Responses;

namespace UserStore.Business.Usr;

public interface IUsrService
{
    Task<UserResponse> RegisterAsync(RegisterUserRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string token);
    Task<UserProfileResponse> GetProfileAsync(int userId);
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task DeleteUserAsync(int userId);
    
}
