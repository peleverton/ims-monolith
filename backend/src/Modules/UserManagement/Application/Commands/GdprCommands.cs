using IMS.Modular.Modules.UserManagement.Application.DTOs;
using MediatR;

namespace IMS.Modular.Modules.UserManagement.Application.Commands;

// ── Data Export ───────────────────────────────────────────────────────────────

/// <summary>US-089: Collect all personal data for a user (LGPD art. 18 III).</summary>
public record GetDataExportQuery(Guid UserId) : IRequest<DataExportDto?>;

// ── Delete Request ────────────────────────────────────────────────────────────

/// <summary>
/// US-089: Initiates soft-delete + schedules hard-delete in 30 days.
/// Returns the created DeleteRequestDto or null if user not found.
/// </summary>
public record RequestDeletionCommand(Guid UserId, Guid RequesterId) : IRequest<DeleteRequestDto?>;

/// <summary>
/// US-089: Admin cancels a pending delete request, reactivating the user.
/// Returns true on success; false if request not found or not pending.
/// </summary>
public record CancelDeletionCommand(Guid UserId, Guid AdminId) : IRequest<bool>;
