using IMS.Modular.Shared.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IMS.Modular.Shared.HealthChecks;

/// <summary>
/// Health check for the cache service (ICacheService).
/// Performs a simple set/get/remove cycle to verify cache connectivity.
/// Works with both InMemoryCacheService and RedisCacheService.
/// </summary>
public sealed class CacheHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;
    private const string HealthCheckKey = "__health_check__";

    public CacheHealthCheck(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Write a test value
            await _cacheService.SetAsync(HealthCheckKey, "ok", TimeSpan.FromSeconds(30));

            // Read it back
            var value = await _cacheService.GetAsync<string>(HealthCheckKey);

            // Clean up
            await _cacheService.RemoveAsync(HealthCheckKey);

            if (value == "ok")
            {
                return HealthCheckResult.Healthy("Cache is operational");
            }

            return HealthCheckResult.Degraded(
                description: "Cache set/get cycle returned unexpected value");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "Cache is not operational",
                exception: ex);
        }
    }
}
