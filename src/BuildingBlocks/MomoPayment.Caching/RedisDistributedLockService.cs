using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MomoPayment.Caching;

public interface IDistributedLockHandle : IDisposable, IAsyncDisposable { }

public interface IDistributedLockService
{
    Task<bool> AcquireLockAsync(string resourceKey, string lockValue, TimeSpan expiry, TimeSpan timeout, TimeSpan retryInterval);
    Task<bool> ReleaseLockAsync(string resourceKey, string lockValue);
    Task<IDistributedLockHandle?> AcquireLocksInOrderAsync(IEnumerable<string> resourceKeys, TimeSpan expiry, TimeSpan timeout);
}

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(IConnectionMultiplexer redis, ILogger<RedisDistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase GetDb() => _redis.GetDatabase();

    public async Task<bool> AcquireLockAsync(
        string resourceKey,
        string lockValue,
        TimeSpan expiry,
        TimeSpan timeout,
        TimeSpan retryInterval)
    {
        var db = GetDb();
        var lockKey = $"lock:{resourceKey}";
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                bool acquired = await db.LockTakeAsync(lockKey, lockValue, expiry);
                if (acquired)
                {
                    _logger.LogDebug("Lock acquired for key: {Key}", lockKey);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error attempting to acquire lock for {Key}", lockKey);
            }

            await Task.Delay(retryInterval);
        }

        _logger.LogWarning("Failed to acquire lock for {Key} within timeout {Timeout}", lockKey, timeout);
        return false;
    }

    public async Task<bool> ReleaseLockAsync(string resourceKey, string lockValue)
    {
        var db = GetDb();
        var lockKey = $"lock:{resourceKey}";
        try
        {
            bool released = await db.LockReleaseAsync(lockKey, lockValue);
            _logger.LogDebug("Lock release for {Key}: {Released}", lockKey, released);
            return released;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for {Key}", lockKey);
            return false;
        }
    }

    /// <summary>
    /// Acquires distributed locks on multiple resources in deterministic sorted order to prevent deadlocks.
    /// Returns an IDisposable lock handle that releases all acquired locks on Dispose.
    /// </summary>
    public async Task<IDistributedLockHandle?> AcquireLocksInOrderAsync(
        IEnumerable<string> resourceKeys,
        TimeSpan expiry,
        TimeSpan timeout)
    {
        // Order keys alphabetically to eliminate circular deadlock wait condition
        var sortedKeys = resourceKeys.Distinct().OrderBy(k => k, StringComparer.Ordinal).ToList();
        var lockValue = Guid.NewGuid().ToString("N");
        var acquiredKeys = new List<string>();

        foreach (var key in sortedKeys)
        {
            bool success = await AcquireLockAsync(
                key,
                lockValue,
                expiry,
                timeout,
                TimeSpan.FromMilliseconds(50));

            if (success)
            {
                acquiredKeys.Add(key);
            }
            else
            {
                // Rollback: release all previously acquired locks in this attempt
                foreach (var acq in acquiredKeys)
                {
                    await ReleaseLockAsync(acq, lockValue);
                }
                return null;
            }
        }

        return new LockReleaser(this, acquiredKeys, lockValue);
    }

    private sealed class LockReleaser : IDistributedLockHandle
    {
        private readonly RedisDistributedLockService _service;
        private readonly List<string> _keys;
        private readonly string _lockValue;
        private bool _disposed;

        public LockReleaser(RedisDistributedLockService service, List<string> keys, string lockValue)
        {
            _service = service;
            _keys = keys;
            _lockValue = lockValue;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var key in _keys.AsEnumerable().Reverse())
            {
                _ = _service.ReleaseLockAsync(key, _lockValue);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var key in _keys.AsEnumerable().Reverse())
            {
                await _service.ReleaseLockAsync(key, _lockValue);
            }
        }
    }
}
