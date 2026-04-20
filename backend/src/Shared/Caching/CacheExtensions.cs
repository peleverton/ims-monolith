using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Behaviors;
using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;

namespace IMS.Modular.Shared.Caching;

/// <summary>
/// Extension methods for registering cache services and output caching policies.
/// US-008: Output Caching + Redis Distributed Cache.
///
/// Auto-detects Redis availability:
///   - If ConnectionStrings:Redis is configured → uses Redis (IDistributedCache + RedisCacheService)
///   - Otherwise → uses in-memory fallback (IMemoryCache + InMemoryCacheService)
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Output Caching policy names. Use these constants when applying [OutputCache] or .CacheOutput().
    /// </summary>
    public static class Policies
    {
        /// <summary>Analytics endpoints — 10 minute cache (reports, summaries).</summary>
        public const string Analytics = "Analytics";

        /// <summary>User data endpoints — 5 minute cache (profiles, lists).</summary>
        public const string Users = "Users";

        /// <summary>Inventory endpoints — 30 second cache (stock levels, products).</summary>
        public const string Inventory = "Inventory";

        /// <summary>Default policy — 60 second cache.</summary>
        public const string Default = "Default";
    }

    /// <summary>
    /// Registers ICacheService (Redis or in-memory), IDistributedCache, IMemoryCache,
    /// and ASP.NET Core Output Caching with named policies.
    /// </summary>
    public static IServiceCollection AddImsCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var useRedis = !string.IsNullOrWhiteSpace(redisConnectionString);

        // Always register IMemoryCache (used by output caching and as fallback)
        services.AddMemoryCache();

        if (useRedis)
        {
            // Redis-backed distributed cache (IDistributedCache → StackExchangeRedis)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "IMS:";
            });

            // Register IConnectionMultiplexer for RemoveByPrefixAsync support
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString!));

            // ICacheService → RedisCacheService (wraps IDistributedCache with JSON serialization)
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // In-memory fallback (no Redis configured)
            services.AddDistributedMemoryCache(); // IDistributedCache backed by IMemoryCache

            // ICacheService → InMemoryCacheService
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }

        // Output Caching — always in-memory (per-instance, not distributed)
        // Output cache handles HTTP response caching; ICacheService handles application-level caching
        services.AddOutputCache(options =>
        {
            ConfigureOutputCachePolicies(options);
        });

        return services;
    }

    /// <summary>
    /// Configures named output caching policies.
    /// </summary>
    private static void ConfigureOutputCachePolicies(OutputCacheOptions options)
    {
        // Default base policy — no caching unless opted in
        options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);

        // Analytics — 10 minutes (reports, summaries, dashboards)
        options.AddPolicy(Policies.Analytics, builder =>
        {
            builder.Expire(TimeSpan.FromMinutes(10));
            builder.Tag("analytics");
        });

        // Users — 5 minutes (user profiles, user lists)
        options.AddPolicy(Policies.Users, builder =>
        {
            builder.Expire(TimeSpan.FromMinutes(5));
            builder.Tag("users");
        });

        // Inventory — 30 seconds (stock levels need fresher data)
        options.AddPolicy(Policies.Inventory, builder =>
        {
            builder.Expire(TimeSpan.FromSeconds(30));
            builder.Tag("inventory");
        });

        // Default — 60 seconds
        options.AddPolicy(Policies.Default, builder =>
        {
            builder.Expire(TimeSpan.FromSeconds(60));
            builder.Tag("default");
        });
    }
}
