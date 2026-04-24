using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Issues.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-067: Loga issues com DueDate vencida e status Open/InProgress.
/// Um futuro status Overdue pode ser adicionado ao enum IssueStatus.
/// Roda a cada 6h.
/// </summary>
public class OverdueIssuesJob(IssuesDbContext issues, ILogger<OverdueIssuesJob> logger)
{
    private static readonly IssueStatus[] ActiveStatuses = [IssueStatus.Open, IssueStatus.InProgress];

    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        var overdue = await issues.Issues
            .Where(i => i.DueDate != null && i.DueDate < now && ActiveStatuses.Contains(i.Status))
            .ToListAsync();

        if (overdue.Count == 0)
        {
            logger.LogInformation("[OverdueIssuesJob] No overdue issues found");
            return;
        }

        foreach (var issue in overdue)
            logger.LogWarning("[OverdueIssuesJob] Issue {Id} '{Title}' overdue since {DueDate}",
                issue.Id, issue.Title, issue.DueDate);

        // TODO: publicar IssueOverdueEvent via domínio quando IssueStatus.Overdue for adicionado
        logger.LogInformation("[OverdueIssuesJob] Logged {Count} overdue issues", overdue.Count);
    }
}
