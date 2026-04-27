namespace IMS.Modular.Modules.Search.Domain;

/// <summary>US-071: A single search result item from any module.</summary>
public record SearchResultItem(
    string Module,   // "inventory" | "issues"
    string Type,     // "product" | "issue"
    string Id,
    string Title,
    string? Description,
    double Score
);

/// <summary>US-071: Paginated search response.</summary>
public record SearchResponse(
    IReadOnlyList<SearchResultItem> Results,
    long Total
);
