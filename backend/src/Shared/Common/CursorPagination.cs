using Microsoft.EntityFrameworkCore;
using System.Text;

namespace IMS.Modular.Shared.Common;

/// <summary>
/// US-087: Cursor-based (keyset) pagination result.
/// Does not include TotalCount — only nextCursor and hasMore.
/// </summary>
public record CursorPagedResult<T>(
    List<T> Items,
    string? NextCursor,
    bool HasMore);

/// <summary>
/// US-087: Cursor encoding/decoding for keyset pagination.
/// Cursor format: base64(createdAt.Ticks:id)
/// </summary>
public static class CursorEncoder
{
    public static string Encode(DateTime createdAt, Guid id)
    {
        var raw = $"{createdAt.Ticks:D20}:{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static (DateTime CreatedAt, Guid Id)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(':');
            if (parts.Length < 2) return null;

            if (!long.TryParse(parts[0], out var ticks)) return null;
            if (!Guid.TryParse(parts[1], out var id)) return null;

            return (new DateTime(ticks, DateTimeKind.Utc), id);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// US-087: Extension method for keyset (cursor-based) pagination on IQueryable.
/// Assumes query is ordered by (CreatedAt DESC, Id ASC) — deterministic ordering.
/// </summary>
public static class KeysetPaginationExtensions
{
    public static async Task<CursorPagedResult<T>> ToKeysetPagedAsync<T>(
        this IQueryable<T> query,
        string? cursor,
        int pageSize,
        Func<T, DateTime> getCreatedAt,
        Func<T, Guid> getId,
        CancellationToken ct = default)
        where T : class
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Fetch one extra item to determine hasMore
        var items = await query.Take(pageSize + 1).ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items[^1];
            nextCursor = CursorEncoder.Encode(getCreatedAt(last), getId(last));
        }

        return new CursorPagedResult<T>(items, nextCursor, hasMore);
    }
}
