using IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;

namespace IMS.Modular.Modules.MarkdownOptimizer.Application.Services;

/// <summary>
/// Motor de regras de markdown. Avalia regras em ordem de prioridade (first-match wins).
/// </summary>
public sealed class MarkdownEngine
{
    /// <summary>
    /// Encontra a primeira regra aplicável ao produto, dado dias até validade e estoque atual.
    /// Retorna null se nenhuma regra se aplica.
    /// </summary>
    public MarkdownRule? Evaluate(IReadOnlyList<MarkdownRule> orderedRules, int daysToExpiry, int currentStock)
    {
        foreach (var rule in orderedRules)
        {
            if (rule.Matches(daysToExpiry, currentStock))
                return rule;
        }
        return null;
    }

    /// <summary>
    /// Calcula o preço com desconto aplicado.
    /// </summary>
    public decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountPercent)
    {
        var discount = originalPrice * (discountPercent / 100m);
        return Math.Round(originalPrice - discount, 2);
    }
}
