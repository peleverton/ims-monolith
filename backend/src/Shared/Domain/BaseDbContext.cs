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
}
