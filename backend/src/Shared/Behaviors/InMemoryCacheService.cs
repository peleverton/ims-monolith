using System.Collections.Concurrent;
using System.Text.Json;
using IMS.Modular.Shared.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace IMS.Modular.Shared.Behaviors;

/// <summary>
/// In-memory cache service implementation.
/// Used as fallback when Redis is not configured.
/// </summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = _cache.Get<T>(key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
            options.SetSlidingExpiration(expiration.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromMinutes(5));

        options.RegisterPostEvictionCallback((k, _, _, _) => _keys.TryRemove(k.ToString()!, out _));

        _keys.TryAdd(key, 0);
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _keys.TryRemove(key, out _);
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keysToRemove = _keys.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _keys.TryRemove(key, out _);
            _cache.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
