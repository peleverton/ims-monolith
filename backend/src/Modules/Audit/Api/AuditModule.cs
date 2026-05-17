using IMS.Modular.Modules.Audit.Application;
using IMS.Modular.Modules.Audit.Application.DTOs;
using IMS.Modular.Modules.Audit.Application.Queries;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.Audit.Api;

/// <summary>
/// US-083: Audit log API — GET /api/audit (Admin only).
/// Accessing audit log is itself self-audited (anti-tampering).
/// </summary>
public static class AuditModule
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization(Policies.CanManageUsers);

        group.MapGet("/", GetAuditLogs).WithName("GetAuditLogs");
        group.MapGet("/actions", GetActions).WithName("GetAuditActions");
    }

    private static async Task<IResult> GetAuditLogs(
        IMediator mediator,
        IAuditService auditService,
        ClaimsPrincipal user,
        HttpContext httpContext,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? tenantId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = new GetAuditLogsQuery(pageNumber, pageSize, userId, tenantId, action, from, to);
        var result = await mediator.Send(query, ct);

        // Self-audit: accessing the audit log is itself logged
        var callerUserId = GetUserId(user);
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        await auditService.LogAsync(
            action: AuditActions.AuditLogAccess,
            userId: callerUserId,
            entityType: "AuditLog",
            ipAddress: ip,
            ct: ct);

        return Results.Ok(result);
    }

    private static IResult GetActions() =>
        Results.Ok(new[]
        {
            AuditActions.Login, AuditActions.Logout,
            AuditActions.UserCreated, AuditActions.UserDeleted,
            AuditActions.RoleAssigned, AuditActions.RoleRevoked,
            AuditActions.DataDeleted, AuditActions.AuditLogAccess
        });

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}
