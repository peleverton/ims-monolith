using IMS.Modular.Modules.Audit.Application.DTOs;
using IMS.Modular.Modules.Audit.Application.Queries;
using IMS.Modular.Modules.Audit.Infrastructure;
using IMS.Modular.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Audit.Application.Handlers;

public sealed class GetAuditLogsQueryHandler(AuditDbContext db)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(e => e.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.TenantId))
            query = query.Where(e => e.TenantId == request.TenantId);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(e => e.Action == request.Action);

        if (request.From.HasValue)
            query = query.Where(e => e.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(e => e.Timestamp <= request.To.Value);

        query = query.OrderByDescending(e => e.Timestamp);

        var paged = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);

        var dtos = paged.Items.Select(e => new AuditLogEntryDto(
            e.Id, e.UserId, e.TenantId, e.Action,
            e.EntityType, e.EntityId, e.OldValue, e.NewValue,
            e.IpAddress, e.Timestamp)).ToList();

        return new PagedResult<AuditLogEntryDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}
