using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using TrafficLightCacheData.Settings;

namespace TrafficLightCacheData;

public class TrafficLightCacheDbContext : IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public TrafficLightCacheDbContext(IOptions<TrafficLightCacheDbSettings> cacheSettings)
    {
        var settings = cacheSettings.Value;

        var config = new ConfigurationOptions
        {
            EndPoints = { $"{settings.Host}:{settings.Port}" },
            Password = string.IsNullOrWhiteSpace(settings.Password) ? null : settings.Password,
            DefaultDatabase = settings.Database,
            AbortOnConnectFail = false
        };

        _redis = ConnectionMultiplexer.Connect(config);
        _database = _redis.GetDatabase();
    }

    public IDatabase Database => _database;

    // Basic operations
    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
        => await _database.StringSetAsync(key, value, expiry);

    public async Task<string?> GetAsync(string key)
        => await _database.StringGetAsync(key);

    public async Task<bool> DeleteAsync(string key)
        => await _database.KeyDeleteAsync(key);

    public async Task<bool> ExistsAsync(string key)
        => await _database.KeyExistsAsync(key);

    // Simple ping for health check
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

    public void Dispose() => _redis.Dispose();
}
