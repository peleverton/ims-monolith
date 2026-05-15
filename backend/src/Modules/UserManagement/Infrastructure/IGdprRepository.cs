using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.UserManagement.Application.DTOs;

namespace IMS.Modular.Modules.UserManagement.Infrastructure;

/// <summary>US-089: LGPD/GDPR data operations — export, delete requests and hard-delete.</summary>
public interface IGdprRepository
{
    /// <summary>Collects all personal data for a user across modules.</summary>
    Task<DataExportDto?> GetDataExportAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Creates a DeleteRequest and applies soft-delete to the user.</summary>
    Task<DeleteRequest?> CreateDeleteRequestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns the active (Pending) delete request for a user, or null.</summary>
    Task<DeleteRequest?> GetPendingDeleteRequestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Cancels a pending delete request and reactivates the user.</summary>
    Task<bool> CancelDeleteRequestAsync(Guid userId, Guid adminId, CancellationToken ct = default);

    /// <summary>Returns all pending delete requests due for execution (hard-delete).</summary>
    Task<IReadOnlyList<DeleteRequest>> GetDueDeleteRequestsAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes hard-delete for a user: removes Issues (authored/assigned), Notifications,
    /// RefreshTokens and the User record itself. Marks the DeleteRequest as Executed.
    /// </summary>
    Task ExecuteHardDeleteAsync(DeleteRequest request, CancellationToken ct = default);
}
