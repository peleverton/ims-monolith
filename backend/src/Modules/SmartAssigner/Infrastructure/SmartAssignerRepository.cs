using System.Data;
using Dapper;
using IMS.Modular.Modules.SmartAssigner.Domain.Models;

namespace IMS.Modular.Modules.SmartAssigner.Infrastructure;

/// <summary>
/// Dapper-based read repository for Smart Assigner.
/// Queries users' workload from Issues table + skills from user tags.
/// </summary>
public sealed class SmartAssignerRepository(IDbConnection connection) : ISmartAssignerRepository
{
    public async Task<List<CandidateScore>> GetCandidatesAsync(CancellationToken ct = default)
    {
        // Get all active users with their issue workload
        const string sql = """
            SELECT
                u."Id" AS "UserId",
                u."Username",
                COALESCE(w."ActiveIssues", 0) AS "ActiveIssues",
                COALESCE(w."OverdueIssues", 0) AS "OverdueIssues",
                COALESCE(w."AvgResolutionHours", 0) AS "AvgResolutionHours"
            FROM "Users" u
            LEFT JOIN (
                SELECT
                    "AssigneeId",
                    SUM(CASE WHEN "Status" IN ('Open', 'InProgress', 'Testing') THEN 1 ELSE 0 END) AS "ActiveIssues",
                    SUM(CASE WHEN "DueDate" < CURRENT_TIMESTAMP
                        AND "Status" NOT IN ('Resolved', 'Closed') THEN 1 ELSE 0 END) AS "OverdueIssues",
                    AVG(CASE WHEN "Status" IN ('Resolved', 'Closed') AND "UpdatedAt" IS NOT NULL
                        THEN EXTRACT(EPOCH FROM ("UpdatedAt" - "CreatedAt")) / 3600.0 END) AS "AvgResolutionHours"
                FROM "Issues"
                WHERE "AssigneeId" IS NOT NULL
                GROUP BY "AssigneeId"
            ) w ON w."AssigneeId" = u."Id"
            WHERE u."IsActive" = true
            ORDER BY COALESCE(w."ActiveIssues", 0) ASC
            """;

        var candidates = (await connection.QueryAsync<CandidateScore>(sql)).ToList();

        // Load skills (tags the user has historically resolved)
        if (candidates.Count > 0)
        {
            const string skillsSql = """
                SELECT DISTINCT
                    i."AssigneeId" AS "UserId",
                    it."Name" AS "Skill"
                FROM "Issues" i
                INNER JOIN "IssueTags" it ON it."IssueId" = i."Id"
                WHERE i."AssigneeId" IS NOT NULL
                  AND i."Status" IN ('Resolved', 'Closed')
                """;

            var skills = (await connection.QueryAsync<(Guid UserId, string Skill)>(skillsSql)).ToList();

            foreach (var candidate in candidates)
            {
                candidate.Skills.AddRange(
                    skills.Where(s => s.UserId == candidate.UserId).Select(s => s.Skill));
            }
        }

        return candidates;
    }

    public async Task<List<string>> GetIssueTagsAsync(Guid issueId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT "Name" FROM "IssueTags" WHERE "IssueId" = @IssueId
            """;

        return (await connection.QueryAsync<string>(sql, new { IssueId = issueId })).ToList();
    }
}
