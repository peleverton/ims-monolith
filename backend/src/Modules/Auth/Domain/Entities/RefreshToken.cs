using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.Auth.Domain.Entities;

/// <summary>
/// US-055: Rotatable refresh token stored in DB.
/// Each use rotates the token (old token is revoked, new one is issued).
/// Supports explicit revocation via logout.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>SHA-256 hash of the raw token string (never store raw).</summary>
    public string TokenHash { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Token that replaced this one (for audit trail).</summary>
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt) =>
        new() { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt };

    public void Revoke(string? replacedByHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByHash;
    }
}
