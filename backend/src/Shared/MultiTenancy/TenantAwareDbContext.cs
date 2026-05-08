using IMS.Modular.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-078/080: DbContext base with full multi-tenancy support.
///
/// Uses snapshot fields (_tenantId, _mtEnabled) set at construction time from the
/// scoped ITenantService. Because EF Core's ParameterExtractingExpressionVisitor
/// reads these fields from the CURRENT context instance (via ContextParameterReplacingExpressionVisitor),
/// each request's DbContext will use its own per-request values.
///
/// Additionally registers TenantModelCacheKeyFactory so that each (DbContextType, TenantId)
/// pair gets a distinct compiled model — preventing any cross-tenant query-plan leaks.
/// </summary>
public abstract class TenantAwareDbContext : BaseDbContext
{
    // Snapshot fields — EF Core reads these from the CURRENT instance at query-execution time.
    private string? _tenantId;
    private bool _mtEnabled;

    protected TenantAwareDbContext(
        DbContextOptions options,
        IMediator mediator,
        ITenantService tenantService)
        : base(options, mediator)
    {
        _tenantId = tenantService.TenantId;
        _mtEnabled = tenantService.IsMultiTenancyEnabled;
    }

    /// <summary>Exposed for IModelCacheKeyFactory.</summary>
    internal string? TenantIdSnapshot => _tenantId;

    protected string? CurrentTenantId => _tenantId;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_mtEnabled && _tenantId is not null)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
                .Where(e => e.State == EntityState.Added && e.Entity.TenantId is null))
            {
                entry.Entity.TenantId = _tenantId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        // References to instance fields (_mtEnabled, _tenantId).
        // EF Core's ContextParameterReplacingExpressionVisitor replaces the
        // captured `this` with the CURRENT context at query-execution time,
        // so _tenantId and _mtEnabled are always read from the current scope.
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e =>
                !_mtEnabled               // feature off → no filter
                || e.TenantId == null     // shared/system record
                || e.TenantId == _tenantId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Register the tenant-aware model cache key factory so EF Core keeps
        // separate compiled models per (DbContextType, TenantId).
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
    }
}

/// <summary>
/// Ensures EF Core caches a distinct compiled model per (DbContextType, TenantId).
/// Without this, the first tenant's compiled query plan would be reused for subsequent tenants.
/// </summary>
internal sealed class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is TenantAwareDbContext tenantCtx)
            return (context.GetType(), tenantCtx.TenantIdSnapshot, designTime);

        return (context.GetType(), designTime);
    }
}

/// <summary>Marker interface para entidades com isolamento por tenant.</summary>
public interface ITenantEntity
{
    string? TenantId { get; set; }
}
