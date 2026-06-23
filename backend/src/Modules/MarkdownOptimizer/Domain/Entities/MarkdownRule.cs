using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;

/// <summary>
/// Regra de markdown (desconto) automático baseada em proximidade de validade.
/// Avaliadas em ordem de prioridade (menor = avalia primeiro, first-match wins).
/// </summary>
public class MarkdownRule : BaseEntity
{
    public string Name { get; private set; } = null!;
    public int Priority { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Condições ──────────────────────────────────────────────────
    /// <summary>Dias restantes até a validade (<=). Ex: 7 = "faltam 7 dias ou menos".</summary>
    public int DaysToExpiryThreshold { get; private set; }

    /// <summary>Estoque mínimo para ativar a regra (>=). Evita markdown de última unidade.</summary>
    public int MinimumStockThreshold { get; private set; }

    // ── Ação ───────────────────────────────────────────────────────
    /// <summary>Percentual de desconto a aplicar (0-100). Ex: 30 = 30% off.</summary>
    public decimal DiscountPercent { get; private set; }

    private MarkdownRule() { }

    public MarkdownRule(
        string name,
        int priority,
        int daysToExpiryThreshold,
        int minimumStockThreshold,
        decimal discountPercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (discountPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Deve ser entre 0 e 100.");
        if (daysToExpiryThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(daysToExpiryThreshold));

        Name = name;
        Priority = priority;
        DaysToExpiryThreshold = daysToExpiryThreshold;
        MinimumStockThreshold = minimumStockThreshold;
        DiscountPercent = discountPercent;
    }

    public void Update(string name, int priority, int daysToExpiryThreshold, int minimumStockThreshold, decimal discountPercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (discountPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent));

        Name = name;
        Priority = priority;
        DaysToExpiryThreshold = daysToExpiryThreshold;
        MinimumStockThreshold = minimumStockThreshold;
        DiscountPercent = discountPercent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    /// <summary>Avalia se esta regra se aplica ao produto.</summary>
    public bool Matches(int daysToExpiry, int currentStock)
        => IsActive
           && daysToExpiry <= DaysToExpiryThreshold
           && currentStock >= MinimumStockThreshold;
}
