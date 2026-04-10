using System.Data;
using Dapper;
using IMS.Modular.Modules.InventoryIssues.Domain.Entities;
using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.InventoryIssues.Infrastructure;

// ── EF Core Write Repository ─────────────────────────────────────────────

public interface IInventoryIssueRepository
{
    Task<InventoryIssue?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(InventoryIssue issue, CancellationToken ct = default);
    Task UpdateAsync(InventoryIssue issue, CancellationToken ct = default);
    Task DeleteAsync(InventoryIssue issue, CancellationToken ct = default);
}

public class InventoryIssueRepository(InventoryIssuesDbContext db) : IInventoryIssueRepository
{
    public Task<InventoryIssue?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.InventoryIssues.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InventoryIssue issue, CancellationToken ct = default)
    {
        db.InventoryIssues.Add(issue);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(InventoryIssue issue, CancellationToken ct = default)
    {
        db.InventoryIssues.Update(issue);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(InventoryIssue issue, CancellationToken ct = default)
    {
        db.InventoryIssues.Remove(issue);
        await db.SaveChangesAsync(ct);
    }
}

// ── Dapper Read Repository ───────────────────────────────────────────────

file static class GH
{
    internal static string Up(Guid id) => id.ToString().ToUpperInvariant();
    internal static string? Up(Guid? id) => id.HasValue ? id.Value.ToString().ToUpperInvariant() : null;
}

public interface IInventoryIssueReadRepository
{
    Task<InventoryIssueReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<InventoryIssueSummaryDto>> GetPagedAsync(
        int page, int pageSize,
        InventoryIssueStatus? status = null,
        InventoryIssuePriority? priority = null,
        InventoryIssueType? type = null,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? reporterId = null,
        Guid? assigneeId = null,
        string? search = null,
        CancellationToken ct = default);
    Task<PagedResult<InventoryIssueSummaryDto>> GetOverdueAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<InventoryIssueSummaryDto>> GetHighPriorityAsync(int page, int pageSize, CancellationToken ct = default);
    Task<InventoryIssueStatsDto> GetStatisticsAsync(CancellationToken ct = default);
}

public class InventoryIssueReadRepository(IDbConnection connection) : IInventoryIssueReadRepository
{
    public async Task<InventoryIssueReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Title, Description, Type, Priority, Status,
                   ProductId, LocationId, ReporterId, AssigneeId,
                   AffectedQuantity, EstimatedLoss, DueDate, ResolvedAt,
                   ResolutionNotes, CreatedAt, UpdatedAt
            FROM InventoryIssues
            WHERE UPPER(Id) = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<InventoryIssueReadDto>(sql, new { Id = GH.Up(id) });
    }

    public async Task<PagedResult<InventoryIssueSummaryDto>> GetPagedAsync(
        int page, int pageSize,
        InventoryIssueStatus? status = null,
        InventoryIssuePriority? priority = null,
        InventoryIssueType? type = null,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? reporterId = null,
        Guid? assigneeId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (status.HasValue)  { where.Add("Status = @Status");         p.Add("Status",     status.Value.ToString()); }
        if (priority.HasValue){ where.Add("Priority = @Priority");      p.Add("Priority",   priority.Value.ToString()); }
        if (type.HasValue)    { where.Add("Type = @Type");              p.Add("Type",       type.Value.ToString()); }
        if (productId.HasValue) { where.Add("UPPER(ProductId) = @ProductId"); p.Add("ProductId", GH.Up(productId)); }
        if (locationId.HasValue){ where.Add("UPPER(LocationId) = @LocationId"); p.Add("LocationId", GH.Up(locationId)); }
        if (reporterId.HasValue){ where.Add("UPPER(ReporterId) = @ReporterId"); p.Add("ReporterId", GH.Up(reporterId)); }
        if (assigneeId.HasValue){ where.Add("UPPER(AssigneeId) = @AssigneeId"); p.Add("AssigneeId", GH.Up(assigneeId)); }
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(Title LIKE @Search OR Description LIKE @Search)");
            p.Add("Search", $"%{search}%");
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        var countSql = $"SELECT COUNT(*) FROM InventoryIssues {whereClause}";
        var dataSql = $"""
            SELECT Id, Title, Type, Priority, Status, ProductId, LocationId,
                   ReporterId, AssigneeId, AffectedQuantity, DueDate, CreatedAt, UpdatedAt
            FROM InventoryIssues {whereClause}
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        p.Add("PageSize", pageSize);
        p.Add("Offset", (page - 1) * pageSize);

        var total = await connection.ExecuteScalarAsync<int>(countSql, p);
        var items = (await connection.QueryAsync<InventoryIssueSummaryDto>(dataSql, p)).AsList();
        return new PagedResult<InventoryIssueSummaryDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<InventoryIssueSummaryDto>> GetOverdueAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("Now", DateTime.UtcNow.ToString("o"));
        p.Add("PageSize", pageSize);
        p.Add("Offset", (page - 1) * pageSize);

        const string countSql = "SELECT COUNT(*) FROM InventoryIssues WHERE DueDate < @Now AND Status NOT IN ('Resolved','Closed')";
        const string dataSql = """
            SELECT Id, Title, Type, Priority, Status, ProductId, LocationId,
                   ReporterId, AssigneeId, AffectedQuantity, DueDate, CreatedAt, UpdatedAt
            FROM InventoryIssues
            WHERE DueDate < @Now AND Status NOT IN ('Resolved','Closed')
            ORDER BY DueDate ASC
            LIMIT @PageSize OFFSET @Offset
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, p);
        var items = (await connection.QueryAsync<InventoryIssueSummaryDto>(dataSql, p)).AsList();
        return new PagedResult<InventoryIssueSummaryDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<InventoryIssueSummaryDto>> GetHighPriorityAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("PageSize", pageSize);
        p.Add("Offset", (page - 1) * pageSize);

        const string countSql = "SELECT COUNT(*) FROM InventoryIssues WHERE Priority IN ('High','Critical') AND Status NOT IN ('Resolved','Closed')";
        const string dataSql = """
            SELECT Id, Title, Type, Priority, Status, ProductId, LocationId,
                   ReporterId, AssigneeId, AffectedQuantity, DueDate, CreatedAt, UpdatedAt
            FROM InventoryIssues
            WHERE Priority IN ('High','Critical') AND Status NOT IN ('Resolved','Closed')
            ORDER BY Priority DESC, CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, p);
        var items = (await connection.QueryAsync<InventoryIssueSummaryDto>(dataSql, p)).AsList();
        return new PagedResult<InventoryIssueSummaryDto>(items, total, page, pageSize);
    }

    public async Task<InventoryIssueStatsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(*) AS Total,
                SUM(CASE WHEN Status = 'Open' THEN 1 ELSE 0 END) AS Open,
                SUM(CASE WHEN Status = 'InProgress' THEN 1 ELSE 0 END) AS InProgress,
                SUM(CASE WHEN Status = 'Resolved' THEN 1 ELSE 0 END) AS Resolved,
                SUM(CASE WHEN Status = 'Closed' THEN 1 ELSE 0 END) AS Closed,
                SUM(CASE WHEN Status = 'Reopened' THEN 1 ELSE 0 END) AS Reopened,
                SUM(CASE WHEN Priority = 'Low' THEN 1 ELSE 0 END) AS LowPriority,
                SUM(CASE WHEN Priority = 'Medium' THEN 1 ELSE 0 END) AS MediumPriority,
                SUM(CASE WHEN Priority = 'High' THEN 1 ELSE 0 END) AS HighPriority,
                SUM(CASE WHEN Priority = 'Critical' THEN 1 ELSE 0 END) AS CriticalPriority,
                SUM(CASE WHEN DueDate < CURRENT_TIMESTAMP AND Status NOT IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS Overdue
            FROM InventoryIssues
            """;

        var row = await connection.QuerySingleAsync<dynamic>(sql);

        var byType = (await connection.QueryAsync<(string Type, int Count)>(
            "SELECT Type, COUNT(*) AS Count FROM InventoryIssues GROUP BY Type")).AsList();

        return new InventoryIssueStatsDto(
            (int)(row.Total ?? 0),
            (int)(row.Open ?? 0),
            (int)(row.InProgress ?? 0),
            (int)(row.Resolved ?? 0),
            (int)(row.Closed ?? 0),
            (int)(row.Reopened ?? 0),
            (int)(row.LowPriority ?? 0),
            (int)(row.MediumPriority ?? 0),
            (int)(row.HighPriority ?? 0),
            (int)(row.CriticalPriority ?? 0),
            (int)(row.Overdue ?? 0),
            byType.ToDictionary(x => x.Type, x => x.Count));
    }
}

// ── Read DTOs ─────────────────────────────────────────────────────────────

public sealed class InventoryIssueReadDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? AssigneeId { get; set; }
    public int? AffectedQuantity { get; set; }
    public decimal? EstimatedLoss { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class InventoryIssueSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? AssigneeId { get; set; }
    public int? AffectedQuantity { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record InventoryIssueStatsDto(
    int Total,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int Reopened,
    int LowPriority,
    int MediumPriority,
    int HighPriority,
    int CriticalPriority,
    int Overdue,
    Dictionary<string, int> ByType);
