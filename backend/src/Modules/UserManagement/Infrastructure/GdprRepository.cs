using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Notifications.Infrastructure;
using IMS.Modular.Modules.UserManagement.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

/// <summary>
/// US-089: LGPD/GDPR repository — crosses module DB boundaries intentionally
/// because GDPR data portability and erasure are cross-cutting concerns.
/// </summary>
public sealed class GdprRepository(
    AuthDbContext auth,
    IssuesDbContext issues,
    NotificationsDbContext notifications) : IGdprRepository
{
    // ── Data Export ──────────────────────────────────────────────────────────

    public async Task<DataExportDto?> GetDataExportAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await auth.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return null;

        var userInfo = new UserExportInfo(
            user.Id.ToString(),
            user.Username,
            user.Email,
            user.FullName,
            [.. user.UserRoles.Select(ur => ur.Role.Name)],
            user.IsActive,
            user.LastLoginAt,
            user.CreatedAt);

        // Issues where user is reporter or assignee
        var userIssues = await issues.Issues
            .Include(i => i.Comments)
            .Include(i => i.Activities)
            .AsNoTracking()
            .IgnoreQueryFilters()  // bypass tenant filter — export ALL user data regardless of tenant
            .Where(i => i.ReporterId == userId || i.AssigneeId == userId)
            .ToListAsync(ct);

        var issueItems = userIssues.Select(i => new IssueExportItem(
            i.Id.ToString(),
            i.Title,
            i.Description,
            i.Status.ToString(),
            i.Priority.ToString(),
            i.ReporterId == userId ? "reporter" : "assignee",
            i.CreatedAt)).ToList();

        var commentItems = userIssues
            .SelectMany(i => i.Comments.Where(c => c.AuthorId == userId)
                .Select(c => new CommentExportItem(i.Id.ToString(), c.Content, c.CreatedAt)))
            .ToList();

        var activityItems = userIssues
            .SelectMany(i => i.Activities.Where(a => a.UserId == userId)
                .Select(a => new ActivityExportItem(i.Id.ToString(), a.ActivityType.ToString(), a.Description, a.Timestamp)))
            .ToList();

        var notifs = await notifications.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .Select(n => new NotificationExportItem(n.Id.ToString(), n.Type, n.Title, n.Body, n.SentAt, n.IsRead))
            .ToListAsync(ct);

        return new DataExportDto(
            userInfo,
            issueItems,
            commentItems,
            activityItems,
            notifs,
            DateTime.UtcNow);
    }

    // ── Delete Request ────────────────────────────────────────────────────────

    public async Task<DeleteRequest?> CreateDeleteRequestAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await auth.Users.FindAsync([userId], ct);
        if (user is null) return null;

        // Idempotent: return existing pending request if one already exists
        var existing = await GetPendingDeleteRequestAsync(userId, ct);
        if (existing is not null) return existing;

        user.IsActive = false;
        user.ClearRefreshToken();
        user.UpdatedAt = DateTime.UtcNow;

        var request = DeleteRequest.Create(userId);
        auth.DeleteRequests.Add(request);
        await auth.SaveChangesAsync(ct);

        return request;
    }

    public async Task<DeleteRequest?> GetPendingDeleteRequestAsync(Guid userId, CancellationToken ct = default)
        => await auth.DeleteRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == DeleteRequestStatus.Pending, ct);

    public async Task<bool> CancelDeleteRequestAsync(Guid userId, Guid adminId, CancellationToken ct = default)
    {
        var request = await GetPendingDeleteRequestAsync(userId, ct);
        if (request is null) return false;

        request.Cancel(adminId);

        var user = await auth.Users.FindAsync([userId], ct);
        if (user is not null)
        {
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await auth.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<DeleteRequest>> GetDueDeleteRequestsAsync(CancellationToken ct = default)
        => await auth.DeleteRequests
            .Where(r => r.Status == DeleteRequestStatus.Pending && r.ScheduledHardDeleteAt <= DateTime.UtcNow)
            .ToListAsync(ct);

    // ── Hard Delete ───────────────────────────────────────────────────────────

    public async Task ExecuteHardDeleteAsync(DeleteRequest request, CancellationToken ct = default)
    {
        var userId = request.UserId;

        // Remove notifications
        var userNotifications = await notifications.Notifications
            .Where(n => n.UserId == userId).ToListAsync(ct);
        notifications.Notifications.RemoveRange(userNotifications);
        await notifications.SaveChangesAsync(ct);

        // Remove issues authored by the user (cascade removes comments/activities/tags)
        var userIssues = await issues.Issues
            .IgnoreQueryFilters()
            .Where(i => i.ReporterId == userId)
            .ToListAsync(ct);
        issues.Issues.RemoveRange(userIssues);
        await issues.SaveChangesAsync(ct);

        // Remove auth data (refresh tokens cascade via FK, then user)
        var refreshTokens = await auth.RefreshTokens
            .Where(t => t.UserId == userId).ToListAsync(ct);
        auth.RefreshTokens.RemoveRange(refreshTokens);

        var user = await auth.Users.FindAsync([userId], ct);
        if (user is not null) auth.Users.Remove(user);

        request.MarkExecuted();
        await auth.SaveChangesAsync(ct);
    }
}
