using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MomoPayment.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisOptions _options;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
    }

    private IDatabase GetDb() => _redis.GetDatabase();

    private string BuildKey(string key) => $"{_options.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = GetDb();
            var value = await db.StringGetAsync(BuildKey(key));
            if (!value.HasValue) return default;
            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache key {Key} from Redis", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = GetDb();
            var json = JsonSerializer.Serialize(value);
            var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpiryMinutes);
            await db.StringSetAsync(BuildKey(key), json, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache key {Key} to Redis", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = GetDb();
            await db.KeyDeleteAsync(BuildKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete cache key {Key} from Redis", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = GetDb();
            return await db.KeyExistsAsync(BuildKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache key {Key} in Redis", key);
            return false;
        }
    }
}
