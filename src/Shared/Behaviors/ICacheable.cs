namespace IMS.Modular.Shared.Behaviors;

/// <summary>
/// Marker interface for cacheable MediatR queries.
/// Implement this on queries that should be automatically cached.
/// </summary>
public interface ICacheable
{
    /// <summary>
    /// Cache key prefix. Combined with request properties to form the full cache key.
    /// Example: "issues-list", "issue-detail"
    /// </summary>
    string CacheKeyPrefix { get; }

    /// <summary>
    /// Cache duration. Defaults to 5 minutes if not specified.
    /// </summary>
    TimeSpan? CacheDuration => null;
}
