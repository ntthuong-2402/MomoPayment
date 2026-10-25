using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MomoPayment.Caching;

public interface IIdempotencyService
{
    Task<bool> TryAcquireKeyAsync(string key, TimeSpan expiry);
    Task ReleaseKeyAsync(string key);
    Task SaveResultAsync<T>(string key, T result, TimeSpan expiry);
    Task<T?> GetResultAsync<T>(string key);
}

public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIdempotencyService> _logger;

    public RedisIdempotencyService(IConnectionMultiplexer redis, ILogger<RedisIdempotencyService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase GetDb() => _redis.GetDatabase();

    public async Task<bool> TryAcquireKeyAsync(string key, TimeSpan expiry)
    {
        var db = GetDb();
        var fullKey = $"idempotency:{key}";
        try
        {
            // SetNX: returns true only if the key did not exist before
            bool acquired = await db.StringSetAsync(fullKey, "PROCESSING", expiry, When.NotExists);
            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check idempotency key {Key}", fullKey);
            return false;
        }
    }

    public async Task ReleaseKeyAsync(string key)
    {
        var db = GetDb();
        var fullKey = $"idempotency:{key}";
        try
        {
            await db.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Released idempotency key {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release idempotency key {Key}", fullKey);
        }
    }

    public async Task SaveResultAsync<T>(string key, T result, TimeSpan expiry)
    {
        var db = GetDb();
        var fullKey = $"idempotency:{key}:result";
        try
        {
            var json = JsonSerializer.Serialize(result);
            await db.StringSetAsync(fullKey, json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save idempotency result for key {Key}", fullKey);
        }
    }

    public async Task<T?> GetResultAsync<T>(string key)
    {
        var db = GetDb();
        var fullKey = $"idempotency:{key}:result";
        try
        {
            var value = await db.StringGetAsync(fullKey);
            if (!value.HasValue) return default;
            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read cached idempotency result for key {Key}", fullKey);
            return default;
        }
    }
}
