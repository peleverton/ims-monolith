using IMS.Modular.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Shared.Domain;

/// <summary>
/// US-022: Centraliza o dispatch de domain events após SaveChangesAsync.
/// Todos os DbContexts do projeto devem herdar desta classe para eliminar
/// o código duplicado de coleta + publicação de eventos.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, IMediator mediator)
    : DbContext(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Fix EF Core issue with OwnsMany entities having client-generated Guid keys:
        // New owned entities added to a tracked collection may incorrectly get EntityState.Modified
        // instead of EntityState.Added when their key is non-default (e.g., Guid.NewGuid()).
        // We detect this by checking if a Modified entry has no original values stored (indicating it was
        // never loaded from DB in this context instance).
        FixNewOwnedEntityStates();

        // Coleta eventos ANTES de salvar (para não perder entidades deletadas)
        var domainEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Persiste primeiro — consistência transacional
        var result = await base.SaveChangesAsync(cancellationToken);

        // Limpa e publica APÓS save bem-sucedido
        domainEntities.ForEach(e => e.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, cancellationToken);

        return result;
    }

    /// <summary>
    /// Fixes EF Core's incorrect state assignment for owned entities with client-generated Guid keys.
    /// When a new owned entity is added to a tracked collection, EF may mark it as Modified instead of Added.
    /// We detect this: for a truly-loaded Modified entity, at least one property's original value differs from
    /// its current value. For a newly-added entity incorrectly marked Modified, all original values
    /// equal all current values (EF copied current→original when it set the state).
    /// </summary>
    private void FixNewOwnedEntityStates()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && e.Metadata.IsOwned())
            .ToList())
        {
            // A genuinely-modified entity has at least one property where original ≠ current.
            // A newly-added entity (incorrectly marked Modified) has original == current for all properties.
            var isGenuinelyModified = entry.Properties
                .Any(p => !Equals(p.OriginalValue, p.CurrentValue));

            if (!isGenuinelyModified)
                entry.State = EntityState.Added;
        }
    }
}
