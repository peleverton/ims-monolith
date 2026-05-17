namespace IMS.Modular.Modules.Audit.Application;

/// <summary>
/// US-083: Well-known audit action identifiers.
/// </summary>
public static class AuditActions
{
    public const string Login          = "Login";
    public const string Logout         = "Logout";
    public const string UserCreated    = "UserCreated";
    public const string UserDeleted    = "UserDeleted";
    public const string RoleAssigned   = "RoleAssigned";
    public const string RoleRevoked    = "RoleRevoked";
    public const string DataDeleted    = "DataDeleted";
    public const string AuditLogAccess = "AuditLogAccess";
}
