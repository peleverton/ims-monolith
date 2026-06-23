namespace IMS.Modular.Modules.MarkdownOptimizer.Application.DTOs;

public record MarkdownRuleDto(
    Guid Id,
    string Name,
    int Priority,
    bool IsActive,
    int DaysToExpiryThreshold,
    int MinimumStockThreshold,
    decimal DiscountPercent,
    DateTime CreatedAt);

public record CreateMarkdownRuleRequest(
    string Name,
    int Priority,
    int DaysToExpiryThreshold,
    int MinimumStockThreshold,
    decimal DiscountPercent);

public record UpdateMarkdownRuleRequest(
    string Name,
    int Priority,
    int DaysToExpiryThreshold,
    int MinimumStockThreshold,
    decimal DiscountPercent);

public record MarkdownApplicationDto(
    Guid Id,
    Guid ProductId,
    string SKU,
    string RuleName,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    decimal DiscountPercent,
    DateTime ExpiryDate,
    int DaysToExpiryAtApplication,
    int StockAtApplication,
    DateTime AppliedAt);
