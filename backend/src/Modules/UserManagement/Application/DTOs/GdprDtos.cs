namespace IMS.Modular.Modules.UserManagement.Application.DTOs;

// ── Data Export ───────────────────────────────────────────────────────────────

/// <summary>US-089: Full personal data export for a user (LGPD art. 18 III).</summary>
public record DataExportDto(
    UserExportInfo User,
    IReadOnlyList<IssueExportItem> Issues,
    IReadOnlyList<CommentExportItem> Comments,
    IReadOnlyList<ActivityExportItem> Activities,
    IReadOnlyList<NotificationExportItem> Notifications,
    DateTime ExportedAt);

public record UserExportInfo(
    string Id,
    string Username,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record IssueExportItem(
    string Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    string Role,       // "reporter" or "assignee"
    DateTime CreatedAt);

public record CommentExportItem(
    string IssueId,
    string Content,
    DateTime CreatedAt);

public record ActivityExportItem(
    string IssueId,
    string ActivityType,
    string? Description,
    DateTime CreatedAt);

public record NotificationExportItem(
    string Id,
    string Type,
    string Title,
    string Body,
    DateTime SentAt,
    bool IsRead);

// ── Delete Request ────────────────────────────────────────────────────────────

/// <summary>US-089: Response after creating or querying a delete request.</summary>
public record DeleteRequestDto(
    string Id,
    string UserId,
    DateTime RequestedAt,
    DateTime ScheduledHardDeleteAt,
    string Status);
