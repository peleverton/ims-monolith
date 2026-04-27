namespace IMS.Modular.Modules.Search.Application;

/// <summary>US-071: Abstraction over the search engine.</summary>
public interface ISearchService
{
    /// <summary>Index a document for full-text search.</summary>
    Task IndexDocumentAsync(string indexName, object document, CancellationToken ct = default);

    /// <summary>Remove a document from the index.</summary>
    Task DeleteDocumentAsync(string indexName, string id, CancellationToken ct = default);

    /// <summary>Search across one or more indexes and return merged results.</summary>
    Task<Domain.SearchResponse> SearchAsync(
        string query,
        string[] modules,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
