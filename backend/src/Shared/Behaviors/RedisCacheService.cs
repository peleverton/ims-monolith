using System.Text.Json;
using IMS.Modular.Shared.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IMS.Modular.Shared.Behaviors;

/// <summary>
/// Redis-backed cache service implementation via IDistributedCache.
/// Used when Redis is configured; falls back to InMemoryCacheService otherwise.
/// US-008: Output Caching + Redis Distributed Cache.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger, IConnectionMultiplexer? multiplexer = null)
    {
        _cache = cache;
        _logger = logger;
        _multiplexer = multiplexer;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);

            if (bytes is null || bytes.Length == 0)
                return default;

            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RedisCacheService] GET failed for key: {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                // Use both absolute and sliding expiration for best behavior
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
                options.SlidingExpiration = expiration.Value > TimeSpan.FromMinutes(1)
                    ? TimeSpan.FromMinutes(expiration.Value.TotalMinutes / 2)
                    : expiration.Value;
            }
            else
            {
                // Default: 5 minutes absolute, 2.5 minutes sliding
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                options.SlidingExpiration = TimeSpan.FromMinutes(2.5);
            }

            await _cache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RedisCacheService] SET failed for key: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RedisCacheService] REMOVE failed for key: {CacheKey}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            return bytes is not null && bytes.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RedisCacheService] EXISTS failed for key: {CacheKey}", key);
            return false;
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (_multiplexer is null)
        {
            _logger.LogWarning("[RedisCacheService] REMOVE_BY_PREFIX skipped — no IConnectionMultiplexer registered. Prefix: {Prefix}", prefix);
            return;
        }

        try
        {
            var db = _multiplexer.GetDatabase();
            var server = _multiplexer.GetServers().FirstOrDefault();
            if (server is null) return;

            var keys = server.Keys(pattern: $"{prefix}:*").ToArray();
            if (keys.Length > 0)
                await db.KeyDeleteAsync(keys);

            _logger.LogInformation("[RedisCacheService] REMOVE_BY_PREFIX removed {Count} keys for prefix: {Prefix}", keys.Length, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RedisCacheService] REMOVE_BY_PREFIX failed for prefix: {Prefix}", prefix);
        }
    }
}
