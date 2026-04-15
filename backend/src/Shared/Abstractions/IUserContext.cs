namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// Provides the current authenticated user's context extracted from JWT claims.
/// Registered as Scoped so it's available per-request throughout the pipeline.
/// </summary>
public interface IUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
