using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Shared.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior that auto-caches query responses.
/// Only applies to requests that implement ICacheable.
/// Uses SHA256 hashing for cache key generation.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache requests that implement ICacheable
        if (request is not ICacheable cacheable)
            return await next();

        var cacheKey = GenerateCacheKey(cacheable.CacheKeyPrefix, request);
        var cachedResponse = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);

        if (cachedResponse is not null)
        {
            _logger.LogInformation(
                "[Cache] HIT for {RequestName} — key: {CacheKey}",
                typeof(TRequest).Name, cacheKey);

            return cachedResponse;
        }

        _logger.LogInformation(
            "[Cache] MISS for {RequestName} — key: {CacheKey}",
            typeof(TRequest).Name, cacheKey);

        var response = await next();

        var duration = cacheable.CacheDuration ?? DefaultCacheDuration;
        await _cache.SetAsync(cacheKey, response, duration, cancellationToken);

        _logger.LogInformation(
            "[Cache] SET for {RequestName} — key: {CacheKey}, TTL: {CacheDurationMinutes}min",
            typeof(TRequest).Name, cacheKey, duration.TotalMinutes);

        return response;
    }

    /// <summary>
    /// Generates a deterministic cache key using SHA256 hash of the request properties.
    /// Format: {prefix}:{sha256-hash}
    /// </summary>
    private static string GenerateCacheKey(string prefix, TRequest request)
    {
        var requestJson = JsonSerializer.Serialize(request);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(requestJson));
        var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
        return $"{prefix}:{hash}";
    }
}
