using IMS.Modular.Modules.Inventory.Domain.Events;
using IMS.Modular.Modules.Search.Application;
using IMS.Modular.Modules.Search.Infrastructure;
using MediatR;

namespace IMS.Modular.Modules.Search.Application.Handlers;

/// <summary>
/// US-071: Index Products into Meilisearch when created.
/// </summary>
public class IndexProductOnCreatedHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ISearchService _search;
    private readonly ILogger<IndexProductOnCreatedHandler> _logger;

    public IndexProductOnCreatedHandler(ISearchService search, ILogger<IndexProductOnCreatedHandler> logger)
    {
        _search = search;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent notification, CancellationToken ct)
    {
        var doc = new SearchDocument
        {
            Id = notification.ProductId.ToString(),
            Type = "product",
            Title = notification.Name,
            Description = $"SKU: {notification.SKU} | Category: {notification.Category}",
            CreatedAt = DateTime.UtcNow,
        };

        await _search.IndexDocumentAsync(MeilisearchService.InventoryIndex, doc, ct);
        _logger.LogInformation("[Search] Indexed product '{Id}'", notification.ProductId);
    }
}
