using UserData.Entities;

namespace UserData.Repositories.Ses;

public interface ISessionRepository
{
    Task<IEnumerable<SessionEntity>> GetUserSessionsAsync(int userId);
    Task<SessionEntity?> GetActiveSessionAsync(int userId);
    Task<SessionEntity?> GetByTokenAsync(string token);
    Task InsertAsync(SessionEntity session);
    Task UpdateAsync(SessionEntity session);
    Task DeleteAsync(SessionEntity session);
    Task DeleteUserSessionsAsync(int userId);
}