using IMS.Modular.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Shared.MultiTenancy;

/// <summary>
/// US-078: DbContext base com suporte a multi-tenancy via Global Query Filter.
/// Aplica automaticamente filtro de TenantId em todas as entidades que implementam ITenantEntity.
/// </summary>
public abstract class TenantAwareDbContext(
    DbContextOptions options,
    IMediator mediator,
    ITenantService tenantService)
    : BaseDbContext(options, mediator)
{
    protected string? CurrentTenantId => tenantService.TenantId;

    protected void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => e.TenantId == null || e.TenantId == CurrentTenantId);
    }
}

/// <summary>Marker interface para entidades com isolamento por tenant.</summary>
public interface ITenantEntity
{
    string? TenantId { get; }
}
