using IMS.Modular.Modules.UserManagement.Infrastructure;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-089: LGPD/GDPR hard-delete job.
/// Runs daily to execute pending delete requests whose grace period has elapsed.
/// Also supports on-demand single-user processing (scheduled via Hangfire delay).
/// </summary>
public class GdprHardDeleteJob(
    IGdprRepository repo,
    ILogger<GdprHardDeleteJob> logger)
{
    /// <summary>
    /// Daily sweep: finds all due delete requests and executes them.
    /// </summary>
    public async Task ExecuteAsync()
    {
        var dueRequests = await repo.GetDueDeleteRequestsAsync();

        if (dueRequests.Count == 0)
        {
            logger.LogInformation("[GdprHardDeleteJob] No pending hard-deletes due.");
            return;
        }

        logger.LogWarning("[GdprHardDeleteJob] Processing {Count} hard-delete request(s).", dueRequests.Count);

        foreach (var request in dueRequests)
        {
            await ProcessSingleAsync(request.UserId);
        }
    }

    /// <summary>
    /// On-demand: process a single user's hard-delete (scheduled via Hangfire delay).
    /// </summary>
    public async Task ProcessSingleAsync(Guid userId)
    {
        var request = await repo.GetPendingDeleteRequestAsync(userId);
        if (request is null)
        {
            logger.LogInformation(
                "[GdprHardDeleteJob] No pending request for UserId={UserId} — skipping (may have been cancelled).",
                userId);
            return;
        }

        if (request.ScheduledHardDeleteAt > DateTime.UtcNow)
        {
            logger.LogWarning(
                "[GdprHardDeleteJob] Request for UserId={UserId} is not due yet (due {Due}). Skipping.",
                userId, request.ScheduledHardDeleteAt);
            return;
        }

        try
        {
            await repo.ExecuteHardDeleteAsync(request);

            logger.LogWarning(
                "[AUDIT][GDPR] HardDelete executed. UserId={UserId} ExecutedAt={ExecutedAt}",
                userId, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GdprHardDeleteJob] Hard-delete failed for UserId={UserId}.", userId);
            throw; // let Hangfire retry
        }
    }
}
