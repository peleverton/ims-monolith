using IMS.Modular.Modules.AnomalyDetection.Domain;
using IMS.Modular.Modules.AnomalyDetection.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.AnomalyDetection.Infrastructure;

public sealed class AnomalyAlertRepository(AnomalyDetectionDbContext db) : IAnomalyAlertRepository
{
    public async Task<List<AnomalyAlert>> GetOpenAlertsAsync(CancellationToken ct = default)
        => await db.AnomalyAlerts
            .Where(a => a.Status == AlertStatus.Open || a.Status == AlertStatus.Escalated)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<AnomalyAlert>> GetAllAsync(int top = 100, CancellationToken ct = default)
        => await db.AnomalyAlerts
            .OrderByDescending(a => a.CreatedAt)
            .Take(top)
            .ToListAsync(ct);

    public async Task<AnomalyAlert?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.AnomalyAlerts.FindAsync([id], ct);

    public async Task AddAsync(AnomalyAlert alert, CancellationToken ct = default)
        => await db.AnomalyAlerts.AddAsync(alert, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
