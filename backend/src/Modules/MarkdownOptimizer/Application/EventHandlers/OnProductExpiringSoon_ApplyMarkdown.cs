using IMS.Modular.Modules.Inventory.Domain.Events;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.MarkdownOptimizer.Application.Services;
using IMS.Modular.Modules.MarkdownOptimizer.Domain;
using IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.MarkdownOptimizer.Application.EventHandlers;

/// <summary>
/// Observer: reage ao ProductExpiringSoonEvent para avaliar e aplicar markdown automático.
/// Totalmente desacoplado do ExpiryCheckJob — dispara via MediatR pub/sub.
/// </summary>
public sealed class OnProductExpiringSoon_ApplyMarkdown : INotificationHandler<ProductExpiringSoonEvent>
{
    private readonly InventoryDbContext _inventory;
    private readonly IMarkdownRuleRepository _ruleRepo;
    private readonly IMarkdownApplicationRepository _applicationRepo;
    private readonly MarkdownEngine _engine;
    private readonly ILogger<OnProductExpiringSoon_ApplyMarkdown> _logger;

    public OnProductExpiringSoon_ApplyMarkdown(
        InventoryDbContext inventory,
        IMarkdownRuleRepository ruleRepo,
        IMarkdownApplicationRepository applicationRepo,
        MarkdownEngine engine,
        ILogger<OnProductExpiringSoon_ApplyMarkdown> logger)
    {
        _inventory = inventory;
        _ruleRepo = ruleRepo;
        _applicationRepo = applicationRepo;
        _engine = engine;
        _logger = logger;
    }

    public async Task Handle(ProductExpiringSoonEvent notification, CancellationToken ct)
    {
        var product = await _inventory.Products
            .FirstOrDefaultAsync(p => p.Id == notification.ProductId, ct);

        if (product is null || !product.IsActive || product.ExpiryDate is null)
            return;

        var daysToExpiry = (int)(product.ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
        if (daysToExpiry < 0) daysToExpiry = 0;

        var rules = await _ruleRepo.GetActiveRulesOrderedAsync(ct);
        var matchedRule = _engine.Evaluate(rules, daysToExpiry, product.CurrentStock);

        if (matchedRule is null)
        {
            _logger.LogDebug("[MarkdownOptimizer] Nenhuma regra aplicável para {SKU} ({Days}d, {Stock} un)",
                product.SKU, daysToExpiry, product.CurrentStock);
            return;
        }

        // Evitar aplicar a mesma regra duas vezes ao mesmo produto
        var alreadyApplied = await _applicationRepo.WasAlreadyAppliedAsync(product.Id, matchedRule.Id, ct);
        if (alreadyApplied)
        {
            _logger.LogDebug("[MarkdownOptimizer] Regra '{Rule}' já aplicada para {SKU}", matchedRule.Name, product.SKU);
            return;
        }

        var originalPrice = product.UnitPrice;
        var discountedPrice = _engine.CalculateDiscountedPrice(originalPrice, matchedRule.DiscountPercent);

        // Aplicar desconto via método de domínio (dispara PriceChangedEvent)
        product.UpdatePricing(discountedPrice, product.CostPrice);

        // Registrar a aplicação para auditoria
        var application = new MarkdownApplication(
            product.Id,
            product.SKU,
            matchedRule.Id,
            matchedRule.Name,
            originalPrice,
            discountedPrice,
            matchedRule.DiscountPercent,
            product.ExpiryDate.Value,
            daysToExpiry,
            product.CurrentStock);

        await _applicationRepo.AddAsync(application, ct);
        await _inventory.SaveChangesAsync(ct);
        await _applicationRepo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[MarkdownOptimizer] Aplicado {Discount}% em {SKU} (regra: {Rule}) — R${Original} → R${New}",
            matchedRule.DiscountPercent, product.SKU, matchedRule.Name, originalPrice, discountedPrice);
    }
}
