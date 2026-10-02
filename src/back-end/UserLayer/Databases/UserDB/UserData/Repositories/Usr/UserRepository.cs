using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserData;
using UserData.Entities;

namespace UserData.Repositories.Usr;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserRepository> _logger;
    private const string domain = "[REPOSITORY][USER]";
    public UserRepository(UserDbContext context, ILogger<UserRepository> logger) 
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserEntity?> GetByIdAsync(int id)
    {
        _logger.LogInformation("{Domain} Retrieving user by ID {UserId}\n", domain, id);
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("{Domain} Retrieving user by email {Email}\n", domain, email);
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        _logger.LogInformation("{Domain} Retrieving user by username {Username}\n", domain, username);
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<IEnumerable<UserEntity>> GetActiveUsersAsync()
    {
        _logger.LogInformation("{Domain} Retrieving active users\n", domain);
        return await _context.Users.Where(u => u.IsActive).ToListAsync();
    }

    public async Task InsertAsync(UserEntity entity)
    {
        _logger.LogInformation("{Domain} Inserting new user\n", domain);
        await _context.Users.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserEntity entity)
    {
        _logger.LogInformation("{Domain} Updating user with ID {UserId}\n", domain, entity.UserId);
        _context.Users.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserEntity entity)
    {
        _logger.LogInformation("{Domain} Deleting user with ID {UserId}\n", domain, entity.UserId);
        _context.Users.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        _logger.LogInformation("{Domain} Checking if user exists with Username: {Username} or Email: {Email}\n", domain, username, email);
        return await _context.Users.AnyAsync(u => u.Username == username || u.Email == email);
    }

    public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
    {
        _logger.LogInformation("{Domain} Retrieving all users\n", domain);
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task DeleteByIdAsync(int userId)
    {
        _logger.LogInformation("{Domain} Deleting user by ID {UserId}\n", domain, userId);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            _logger.LogWarning("{Domain} Attempted to delete non-existent user ID {UserId}\n", domain, userId);
            throw new KeyNotFoundException("User not found.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}