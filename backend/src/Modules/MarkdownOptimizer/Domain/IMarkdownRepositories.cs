using IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;

namespace IMS.Modular.Modules.MarkdownOptimizer.Domain;

public interface IMarkdownRuleRepository
{
    Task<List<MarkdownRule>> GetActiveRulesOrderedAsync(CancellationToken ct = default);
    Task<List<MarkdownRule>> GetAllAsync(CancellationToken ct = default);
    Task<MarkdownRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(MarkdownRule rule, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IMarkdownApplicationRepository
{
    Task<List<MarkdownApplication>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task<List<MarkdownApplication>> GetRecentAsync(int count = 50, CancellationToken ct = default);
    Task AddAsync(MarkdownApplication application, CancellationToken ct = default);
    Task<bool> WasAlreadyAppliedAsync(Guid productId, Guid ruleId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
