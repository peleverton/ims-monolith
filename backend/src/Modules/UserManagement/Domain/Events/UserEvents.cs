namespace IMS.Modular.Modules.UserManagement.Domain.Events;

/// <summary>US-064: Domain events for the UserManagement module.</summary>

public record UserRoleChangedEvent(Guid UserId, string OldRole, string NewRole, DateTime OccurredOn)
{
    public UserRoleChangedEvent(Guid userId, string oldRole, string newRole)
        : this(userId, oldRole, newRole, DateTime.UtcNow) { }
}

public record UserActivatedEvent(Guid UserId, DateTime OccurredOn)
{
    public UserActivatedEvent(Guid userId) : this(userId, DateTime.UtcNow) { }
}

public record UserDeactivatedEvent(Guid UserId, DateTime OccurredOn)
{
    public UserDeactivatedEvent(Guid userId) : this(userId, DateTime.UtcNow) { }
}

public record UserInvitedEvent(Guid UserId, string Username, string Email, string Role, DateTime OccurredOn)
{
    public UserInvitedEvent(Guid userId, string username, string email, string role)
        : this(userId, username, email, role, DateTime.UtcNow) { }
}

public record UserProfileUpdatedEvent(Guid UserId, DateTime OccurredOn)
{
    public UserProfileUpdatedEvent(Guid userId) : this(userId, DateTime.UtcNow) { }
}
