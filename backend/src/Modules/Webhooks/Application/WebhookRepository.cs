using IMS.Modular.Modules.Webhooks.Domain.Entities;
using IMS.Modular.Modules.Webhooks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Webhooks.Application;

public interface IWebhookRepository
{
    Task<List<WebhookRegistration>> GetActiveByEventAsync(string eventName, CancellationToken ct = default);
    Task<List<WebhookRegistration>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<WebhookRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(WebhookRegistration registration, CancellationToken ct = default);
    Task SaveDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class WebhookRepository(WebhooksDbContext db) : IWebhookRepository
{
    public Task<List<WebhookRegistration>> GetActiveByEventAsync(string eventName, CancellationToken ct) =>
        db.WebhookRegistrations
          .Where(r => r.IsActive)
          .ToListAsync(ct)
          .ContinueWith(t => t.Result
              .Where(r => r.ListensTo(eventName))
              .ToList(), ct);

    public Task<List<WebhookRegistration>> GetByOwnerAsync(Guid ownerId, CancellationToken ct) =>
        db.WebhookRegistrations
          .Where(r => r.OwnerId == ownerId)
          .OrderByDescending(r => r.CreatedAt)
          .ToListAsync(ct);

    public Task<WebhookRegistration?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.WebhookRegistrations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(WebhookRegistration registration, CancellationToken ct)
    {
        db.WebhookRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveDeliveryAsync(WebhookDelivery delivery, CancellationToken ct)
    {
        db.WebhookDeliveries.Add(delivery);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
