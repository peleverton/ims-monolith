using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Issues.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-067: Snapshot semanal de KPIs para histórico de tendências.
/// Loga as métricas; persitência em AnalyticsSnapshots será adicionada em US-066+.
/// </summary>
public class AnalyticsSnapshotJob(
    IssuesDbContext issues,
    InventoryDbContext inventory,
    ILogger<AnalyticsSnapshotJob> logger)
{
    public async Task ExecuteAsync()
    {
        var totalIssues = await issues.Issues.CountAsync();
        var openIssues = await issues.Issues.CountAsync(i => i.Status == IMS.Modular.Modules.Issues.Domain.Enums.IssueStatus.Open);
        var resolvedIssues = await issues.Issues.CountAsync(i => i.Status == IMS.Modular.Modules.Issues.Domain.Enums.IssueStatus.Resolved);
        var totalProducts = await inventory.Products.CountAsync();
        var lowStockProducts = await inventory.Products
            .CountAsync(p => p.CurrentStock <= p.MinimumStockLevel);

        logger.LogInformation(
            "[AnalyticsSnapshotJob] Weekly snapshot — Issues Total:{Total} Open:{Open} Resolved:{Resolved} | Products:{Products} LowStock:{LowStock}",
            totalIssues, openIssues, resolvedIssues, totalProducts, lowStockProducts);

        // TODO (post US-066): persistir em tabela AnalyticsSnapshots
        await Task.CompletedTask;
    }
}
