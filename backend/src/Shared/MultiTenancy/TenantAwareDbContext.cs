using IMS.Modular.Shared.Domain;
using IMS.Modular.Shared.FeatureFlags;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-078/081: DbContext base com suporte completo a multi-tenancy.
/// - Aplica global query filter por TenantId em todas as entidades ITenantEntity.
/// - Auto-popula TenantId em entidades novas durante SaveChangesAsync.
/// - US-081 conclusão: comportamento controlado pela feature flag EnableMultiTenancy.
///   Quando desabilitada, queries retornam todos os dados (modo single-tenant / sistema).
/// </summary>
public abstract class TenantAwareDbContext(
    DbContextOptions options,
    IMediator mediator,
    ITenantService tenantService,
    IFeatureManager featureManager)
    : BaseDbContext(options, mediator)
{
    protected string? CurrentTenantId => tenantService.TenantId;

    /// <summary>
    /// Returns true when multi-tenancy is both enabled (feature flag) and a tenant is resolved.
    /// </summary>
    private bool MultiTenancyActive =>
        featureManager.IsEnabledAsync(IMS.Modular.Shared.FeatureFlags.FeatureFlags.EnableMultiTenancy).GetAwaiter().GetResult()
        && CurrentTenantId is not null;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // US-081: Auto-set TenantId on new tenant-aware entities only when feature is active
        if (MultiTenancyActive)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
                .Where(e => e.State == EntityState.Added && e.Entity.TenantId is null))
            {
                entry.Entity.TenantId = CurrentTenantId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        // US-081: The filter is always registered (EF Core requires it at model-build time),
        // but it is a no-op when MultiTenancyActive is false because the condition short-circuits.
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e =>
                !MultiTenancyActive          // feature off → no filter applied
                || e.TenantId == null        // null means shared/system record
                || e.TenantId == CurrentTenantId);
    }
}

/// <summary>Marker interface para entidades com isolamento por tenant.</summary>
public interface ITenantEntity
{
    string? TenantId { get; set; }
}
