using IMS.Modular.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-067: Remove RefreshTokens revogados ou expirados há mais de 30 dias.
/// Roda diariamente à noite.
/// </summary>
public class TokenCleanupJob(AuthDbContext auth, ILogger<TokenCleanupJob> logger)
{
    public async Task ExecuteAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(-30);

        var stale = await auth.RefreshTokens
            .Where(t => (t.RevokedAt != null || t.ExpiresAt < DateTime.UtcNow) && t.CreatedAt < threshold)
            .ToListAsync();

        if (stale.Count == 0)
        {
            logger.LogInformation("[TokenCleanupJob] No stale tokens to remove");
            return;
        }

        auth.RefreshTokens.RemoveRange(stale);
        await auth.SaveChangesAsync();

        logger.LogInformation("[TokenCleanupJob] Removed {Count} stale refresh tokens", stale.Count);
    }
}
