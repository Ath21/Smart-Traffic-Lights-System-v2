using UserData.Entities;

namespace UserData.Repositories.Usr;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(int id);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity?> GetByUsernameAsync(string username);
    Task<IEnumerable<UserEntity>> GetActiveUsersAsync();
    Task InsertAsync(UserEntity entity);
    Task UpdateAsync(UserEntity entity);
    Task DeleteAsync(UserEntity entity);
    Task<bool> ExistsAsync(string username, string email);
    Task<IEnumerable<UserEntity>> GetAllUsersAsync();
    Task DeleteByIdAsync(int userId);
}