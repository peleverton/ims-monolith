using IMS.Modular.Modules.MarkdownOptimizer.Domain;
using IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.MarkdownOptimizer.Infrastructure;

public sealed class MarkdownRuleRepository(MarkdownOptimizerDbContext db) : IMarkdownRuleRepository
{
    public async Task<List<MarkdownRule>> GetActiveRulesOrderedAsync(CancellationToken ct = default)
        => await db.MarkdownRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<List<MarkdownRule>> GetAllAsync(CancellationToken ct = default)
        => await db.MarkdownRules
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<MarkdownRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.MarkdownRules.FindAsync([id], ct);

    public async Task AddAsync(MarkdownRule rule, CancellationToken ct = default)
        => await db.MarkdownRules.AddAsync(rule, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

public sealed class MarkdownApplicationRepository(MarkdownOptimizerDbContext db) : IMarkdownApplicationRepository
{
    public async Task<List<MarkdownApplication>> GetByProductAsync(Guid productId, CancellationToken ct = default)
        => await db.MarkdownApplications
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(ct);

    public async Task<List<MarkdownApplication>> GetRecentAsync(int count = 50, CancellationToken ct = default)
        => await db.MarkdownApplications
            .OrderByDescending(a => a.AppliedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task AddAsync(MarkdownApplication application, CancellationToken ct = default)
        => await db.MarkdownApplications.AddAsync(application, ct);

    public async Task<bool> WasAlreadyAppliedAsync(Guid productId, Guid ruleId, CancellationToken ct = default)
        => await db.MarkdownApplications
            .AnyAsync(a => a.ProductId == productId && a.RuleId == ruleId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
