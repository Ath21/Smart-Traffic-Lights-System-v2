using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserData;
using UserData.Entities;

namespace UserData.Repositories.Ses;


public class SessionRepository : ISessionRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<SessionRepository> _logger;
    private const string domain = "[REPOSITORY][SESSION]";

    public SessionRepository(UserDbContext context, ILogger<SessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SessionEntity>> GetUserSessionsAsync(int userId)
    {
        _logger.LogInformation("{Domain} Retrieving sessions for user {UserId}\n", domain, userId);
        return await _context.Sessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoginTime)
            .ToListAsync();
    }

    public async Task<SessionEntity?> GetActiveSessionAsync(int userId)
    {
        _logger.LogInformation("{Domain} Retrieving active session for user {UserId}\n", domain, userId);
        return await _context.Sessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
    }

    public async Task<SessionEntity?> GetByTokenAsync(string token)
    {
        _logger.LogInformation("{Domain} Retrieving session by token\n", domain);
        return await _context.Sessions.FirstOrDefaultAsync(s => s.Session == token);
    }

    public async Task InsertAsync(SessionEntity session)
    {
        _logger.LogInformation("{Domain} Inserting new session for user {UserId}\n", domain, session.UserId);
        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SessionEntity session)
    {
        _logger.LogInformation("{Domain} Updating session for user {UserId}\n", domain, session.UserId);
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SessionEntity session)
    {
        _logger.LogInformation("{Domain} Deleting session for user {UserId}\n", domain, session.UserId);
        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserSessionsAsync(int userId)
    {
        _logger.LogInformation("{Domain} Deleting all sessions for user {UserId}\n", domain, userId);
        var sessions = await _context.Sessions.Where(s => s.UserId == userId).ToListAsync();
        if (sessions.Any())
        {
            _context.Sessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
        }
    }
}
