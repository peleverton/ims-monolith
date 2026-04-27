using IMS.Modular.Modules.Issues.Domain.Events;
using IMS.Modular.Modules.Search.Application;
using IMS.Modular.Modules.Search.Infrastructure;
using MediatR;

namespace IMS.Modular.Modules.Search.Application.Handlers;

/// <summary>
/// US-071: Index Issues into Meilisearch when created or updated.
/// </summary>
public class IndexIssueOnCreatedHandler : INotificationHandler<IssueCreatedEvent>
{
    private readonly ISearchService _search;
    private readonly ILogger<IndexIssueOnCreatedHandler> _logger;

    public IndexIssueOnCreatedHandler(ISearchService search, ILogger<IndexIssueOnCreatedHandler> logger)
    {
        _search = search;
        _logger = logger;
    }

    public async Task Handle(IssueCreatedEvent notification, CancellationToken ct)
    {
        var doc = new SearchDocument
        {
            Id = notification.IssueId.ToString(),
            Type = "issue",
            Title = notification.Title,
            Description = notification.Priority.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        await _search.IndexDocumentAsync(MeilisearchService.IssuesIndex, doc, ct);
        _logger.LogInformation("[Search] Indexed issue '{Id}'", notification.IssueId);
    }
}
