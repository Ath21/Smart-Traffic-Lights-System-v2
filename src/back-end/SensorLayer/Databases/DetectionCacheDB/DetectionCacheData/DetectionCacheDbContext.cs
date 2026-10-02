using DetectionCacheData.Settings;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DetectionCacheData;

public class DetectionCacheDbContext : IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public DetectionCacheDbContext(IOptions<DetectionCacheDbSettings> cacheSettings)
    {
        var config = new ConfigurationOptions
        {
            EndPoints = { $"{cacheSettings.Value.Host}:{cacheSettings.Value.Port}" },
            Password = string.IsNullOrWhiteSpace(cacheSettings.Value.Password) ? null : cacheSettings.Value.Password,
            DefaultDatabase = cacheSettings.Value.Database,
            AbortOnConnectFail = false
        };

        _redis = ConnectionMultiplexer.Connect(config);
        _database = _redis.GetDatabase();
    }

    public IDatabase Database => _database;

    // ===============================
    // Basic Operations
    // ===============================
    public async Task<bool> SetValueAsync(string key, string value, TimeSpan? expiry = null)
        => await _database.StringSetAsync(key, value, expiry);

    public async Task<string?> GetValueAsync(string key)
        => await _database.StringGetAsync(key);

    public async Task<bool> DeleteKeyAsync(string key)
        => await _database.KeyDeleteAsync(key);

    public async Task<bool> KeyExistsAsync(string key)
        => await _database.KeyExistsAsync(key);

    // ===============================
    // Health Check (Ping)
    // ===============================
    public async Task<bool> CanConnectAsync()
    {
        try
        {
            var result = await _database.PingAsync();
            return result.TotalMilliseconds >= 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _redis.Dispose();
    }
}
