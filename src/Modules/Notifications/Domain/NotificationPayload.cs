namespace IMS.Modular.Modules.Notifications.Domain;

// ── Notification payload ──────────────────────────────────────────────────────

public record NotificationPayload(
    string Type,
    string Title,
    string Message,
    string? ActionUrl = null,
    string? UserId = null,
    IDictionary<string, string>? Metadata = null)
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public DateTime SentAt { get; } = DateTime.UtcNow;
}

// ── Email message ─────────────────────────────────────────────────────────────

public record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = true,
    string[]? Cc = null,
    string[]? Bcc = null);

// ── Notification types ────────────────────────────────────────────────────────

public static class NotificationType
{
    public const string IssueAssigned = "issue.assigned";
    public const string IssueStatusChanged = "issue.status_changed";
    public const string IssueCommentAdded = "issue.comment_added";
    public const string LowStockAlert = "inventory.low_stock";
    public const string OutOfStockAlert = "inventory.out_of_stock";
    public const string InventoryIssueCreated = "inventory_issue.created";
    public const string InventoryIssueAssigned = "inventory_issue.assigned";
    public const string UserActivated = "user.activated";
    public const string UserDeactivated = "user.deactivated";
    public const string UserRoleChanged = "user.role_changed";
    public const string SystemAlert = "system.alert";
}
