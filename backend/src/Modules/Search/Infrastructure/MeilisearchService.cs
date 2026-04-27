using IMS.Modular.Modules.Search.Domain;
using Meilisearch;

namespace IMS.Modular.Modules.Search.Infrastructure;

/// <summary>
/// US-071: Meilisearch implementation of ISearchService.
/// </summary>
public class MeilisearchService : Application.ISearchService
{
    private readonly MeilisearchClient _client;
    private readonly ILogger<MeilisearchService> _logger;

    // Index names per module
    public const string IssuesIndex    = "issues";
    public const string InventoryIndex = "inventory";

    public MeilisearchService(MeilisearchClient client, ILogger<MeilisearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task IndexDocumentAsync(string indexName, object document, CancellationToken ct = default)
    {
        try
        {
            var index = _client.Index(indexName);
            await index.AddDocumentsAsync([document], cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Search] Failed to index document in '{Index}'", indexName);
        }
    }

    public async Task DeleteDocumentAsync(string indexName, string id, CancellationToken ct = default)
    {
        try
        {
            var index = _client.Index(indexName);
            await index.DeleteOneDocumentAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Search] Failed to delete document '{Id}' from '{Index}'", id, indexName);
        }
    }

    public async Task<SearchResponse> SearchAsync(
        string query,
        string[] modules,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var results = new List<SearchResultItem>();
        long total = 0;

        var targetModules = modules.Length == 0
            ? new[] { IssuesIndex, InventoryIndex }
            : modules;

        foreach (var mod in targetModules)
        {
            try
            {
                var index = _client.Index(mod);
                var searchResult = await index.SearchAsync<SearchDocument>(query, new SearchQuery
                {
                    Limit = pageSize,
                    Offset = (page - 1) * pageSize,
                }, ct);

                total += searchResult is Meilisearch.SearchResult<SearchDocument> sr
                    ? sr.EstimatedTotalHits
                    : searchResult.Hits.Count;

                results.AddRange(searchResult.Hits.Select(h => new SearchResultItem(
                    Module: mod,
                    Type: h.Type ?? mod.TrimEnd('s'),
                    Id: h.Id,
                    Title: h.Title,
                    Description: h.Description,
                    Score: 1.0 // Meilisearch doesn't expose score directly
                )));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Search] Search failed on index '{Index}'", mod);
            }
        }

        // Sort by score desc (all same here, but preserves future ranking)
        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        return new SearchResponse(results, total);
    }
}

/// <summary>Generic document shape stored in Meilisearch indexes.</summary>
public record SearchDocument
{
    public string Id { get; init; } = "";
    public string? Type { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public DateTime? CreatedAt { get; init; }
}
