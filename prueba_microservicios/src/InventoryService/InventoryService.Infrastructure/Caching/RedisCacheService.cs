using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InventoryService.Infrastructure.Caching;

public class RedisCacheService : IDistributedCache
{
    private readonly StackExchange.Redis.IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(StackExchange.Redis.IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public byte[]? Get(string key)
    {
        var value = _database.StringGet(key);
        return value.HasValue ? (byte[]?)value : null;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? (byte[]?)value : null;
    }

    public void Refresh(string key)
    {
        _logger.LogWarning("Refresh called for key: {Key}", key);
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        _logger.LogWarning("RefreshAsync called for key: {Key}", key);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _database.KeyDelete(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return _database.KeyDeleteAsync(key);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var expiry = options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromHours(1);
        _database.StringSet(key, value, expiry);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var expiry = options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromHours(1);
        return _database.StringSetAsync(key, value, expiry);
    }
}

