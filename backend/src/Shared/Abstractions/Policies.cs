namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// US-057: Centralized RBAC policy names used across all modules.
/// Apply via .RequireAuthorization(Policies.XYZ) on route groups or endpoints.
/// </summary>
public static class Policies
{
    // ── User Management ──────────────────────────────────────────────────
    /// <summary>Only Admin can manage users, roles and system settings.</summary>
    public const string CanManageUsers = "CanManageUsers";

    // ── Issues ───────────────────────────────────────────────────────────
    /// <summary>Any authenticated user can create and view issues.</summary>
    public const string CanCreateIssue = "CanCreateIssue";

    /// <summary>Admin and Manager can delete or bulk-manage issues.</summary>
    public const string CanManageIssues = "CanManageIssues";

    // ── Inventory ────────────────────────────────────────────────────────
    /// <summary>Admin and Manager can write to inventory (create/update/delete).</summary>
    public const string CanManageInventory = "CanManageInventory";

    /// <summary>Any authenticated user can view inventory data.</summary>
    public const string CanViewInventory = "CanViewInventory";

    // ── Analytics ────────────────────────────────────────────────────────
    /// <summary>Admin and Manager can view analytics dashboards and export data.</summary>
    public const string CanViewAnalytics = "CanViewAnalytics";
}
