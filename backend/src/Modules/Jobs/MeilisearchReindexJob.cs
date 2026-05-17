using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Search.Application;
using IMS.Modular.Modules.Search.Infrastructure;
using IMS.Modular.Shared.Observability;
using Meilisearch;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-088: Weekly Hangfire job that detects drift between the database and Meilisearch indexes.
/// - Compares document counts: DB vs index
/// - Triggers incremental reindex when drift > 1%
/// - Updates Prometheus metrics: meilisearch_drift_ratio, meilisearch_last_reindex_at
/// </summary>
public sealed class MeilisearchReindexJob(
    IssuesDbContext issuesDb,
    InventoryDbContext inventoryDb,
    ISearchService? searchService,
    MeilisearchClient? meilisearchClient,
    ILogger<MeilisearchReindexJob> logger)
{
    private const double DriftThreshold = 0.01; // 1%

    public async Task ExecuteAsync(bool fullReindex = false)
    {
        if (searchService is null || meilisearchClient is null)
        {
            logger.LogWarning("[MeilisearchReindex] Search service not available, skipping reindex job");
            return;
        }

        logger.LogInformation("[MeilisearchReindex] Starting drift check (fullReindex={FullReindex})", fullReindex);

        try
        {
            var issuesDrift = await CheckAndReindexAsync(
                MeilisearchService.IssuesIndex,
                await issuesDb.Issues.CountAsync(),
                fullReindex);

            var inventoryDrift = await CheckAndReindexAsync(
                MeilisearchService.InventoryIndex,
                await inventoryDb.Products.Where(p => p.IsActive).CountAsync(),
                fullReindex);

            var maxDrift = Math.Max(issuesDrift, inventoryDrift);
            OpenTelemetryExtensions.RecordMeilisearchDrift(maxDrift);
            OpenTelemetryExtensions.RecordMeilisearchLastReindex(DateTime.UtcNow);

            logger.LogInformation("[MeilisearchReindex] Completed. Max drift ratio: {Drift:P2}", maxDrift);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MeilisearchReindex] Job failed");
        }
    }

    private async Task<double> CheckAndReindexAsync(string indexName, int dbCount, bool fullReindex)
    {
        try
        {
            var index = meilisearchClient!.Index(indexName);
            var stats = await index.GetStatsAsync();
            var indexCount = (int)(stats.NumberOfDocuments);

            var drift = dbCount > 0
                ? Math.Abs(dbCount - indexCount) / (double)dbCount
                : 0.0;

            logger.LogInformation(
                "[MeilisearchReindex] Index={Index} DB={DbCount} Index={IndexCount} Drift={Drift:P2}",
                indexName, dbCount, indexCount, drift);

            if (fullReindex || drift > DriftThreshold)
            {
                logger.LogInformation("[MeilisearchReindex] Triggering reindex for {Index} (drift={Drift:P2})", indexName, drift);
                await ReindexAsync(indexName);
            }

            return drift;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MeilisearchReindex] Failed to check drift for index '{Index}'", indexName);
            return 0.0;
        }
    }

    private async Task ReindexAsync(string indexName)
    {
        try
        {
            if (indexName == MeilisearchService.IssuesIndex)
            {
                var issues = await issuesDb.Issues
                    .AsNoTracking()
                    .Select(i => new { i.Id, i.Title, i.Description, i.CreatedAt, Type = "issue" })
                    .ToListAsync();

                await searchService!.IndexDocumentAsync(indexName, issues);
            }
            else if (indexName == MeilisearchService.InventoryIndex)
            {
                var products = await inventoryDb.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Select(p => new { p.Id, Title = p.Name, Description = p.Description, p.CreatedAt, Type = "product" })
                    .ToListAsync();

                await searchService!.IndexDocumentAsync(indexName, products);
            }

            logger.LogInformation("[MeilisearchReindex] Reindex completed for {Index}", indexName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MeilisearchReindex] Reindex failed for {Index}", indexName);
        }
    }
}
