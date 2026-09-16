using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartLedger.Domain.Interfaces;
using StackExchange.Redis;

namespace SmartLedger.Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase? _redis;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _redis = redis?.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_redis is not null)
        {
            try
            {
                var value = await _redis.StringGetAsync(key);
                if (value.HasValue)
                    return JsonSerializer.Deserialize<T>((string)value!);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis get failed for {Key}; falling back to memory.", key);
            }
        }

        return _memoryCache.TryGetValue(key, out T? cached) ? cached : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var ttl = expiry ?? TimeSpan.FromMinutes(30);

        if (_redis is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _redis.StringSetAsync(key, json, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis set failed for {Key}; using memory.", key);
            }
        }

        _memoryCache.Set(key, value, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (_redis is not null)
        {
            try
            {
                await _redis.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis delete failed for {Key}.", key);
            }
        }

        _memoryCache.Remove(key);
    }
}
