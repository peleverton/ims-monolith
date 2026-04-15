using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Shared.Common;

/// <summary>
/// Generic paginated response
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

/// <summary>
/// Extension methods for IQueryable pagination and sorting
/// </summary>
public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var count = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, count, pageNumber, pageSize);
    }

    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? sortBy,
        string sortDirection = "asc")
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        var propertyInfo = typeof(T).GetProperty(sortBy,
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (propertyInfo is null)
            return query;

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
        var property = System.Linq.Expressions.Expression.Property(parameter, propertyInfo);
        var lambda = System.Linq.Expressions.Expression.Lambda(property, parameter);

        var methodName = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? "OrderByDescending"
            : "OrderBy";

        var resultExpression = System.Linq.Expressions.Expression.Call(
            typeof(Queryable), methodName,
            [typeof(T), propertyInfo.PropertyType],
            query.Expression,
            System.Linq.Expressions.Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}
