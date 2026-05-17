using IMS.Modular.Modules.Audit.Application.DTOs;
using IMS.Modular.Shared.Common;
using MediatR;

namespace IMS.Modular.Modules.Audit.Application.Queries;

/// <summary>
/// US-083: Query to retrieve audit log entries with filters and pagination.
/// </summary>
public record GetAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? TenantId = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<PagedResult<AuditLogEntryDto>>;
