using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;

/// <summary>
/// Registro de aplicação de markdown — auditoria de cada desconto automático aplicado.
/// </summary>
public class MarkdownApplication : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string SKU { get; private set; } = null!;
    public Guid RuleId { get; private set; }
    public string RuleName { get; private set; } = null!;

    public decimal OriginalPrice { get; private set; }
    public decimal DiscountedPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }

    public DateTime ExpiryDate { get; private set; }
    public int DaysToExpiryAtApplication { get; private set; }
    public int StockAtApplication { get; private set; }

    public DateTime AppliedAt { get; private set; } = DateTime.UtcNow;

    private MarkdownApplication() { }

    public MarkdownApplication(
        Guid productId,
        string sku,
        Guid ruleId,
        string ruleName,
        decimal originalPrice,
        decimal discountedPrice,
        decimal discountPercent,
        DateTime expiryDate,
        int daysToExpiry,
        int stockAtApplication)
    {
        ProductId = productId;
        SKU = sku;
        RuleId = ruleId;
        RuleName = ruleName;
        OriginalPrice = originalPrice;
        DiscountedPrice = discountedPrice;
        DiscountPercent = discountPercent;
        ExpiryDate = expiryDate;
        DaysToExpiryAtApplication = daysToExpiry;
        StockAtApplication = stockAtApplication;
    }
}
