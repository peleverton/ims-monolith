using IMS.Modular.Modules.Notifications.Domain.Entities;
using IMS.Modular.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Notifications.Application;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<List<Notification>> GetByUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class NotificationRepository(NotificationsDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await db.Notifications.AddAsync(notification, ct);

    public Task<List<Notification>> GetByUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct = default)
        => db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
