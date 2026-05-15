using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.UserManagement.Api;

/// <summary>
/// US-089: LGPD/GDPR endpoints — data export and right-to-delete.
/// All endpoints require authentication; self or Admin can operate on own data.
/// </summary>
public class GdprModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/users")
            .WithTags("GDPR / LGPD")
            .RequireAuthorization();

        group.MapGet("/{id:guid}/data-export", GetDataExport)
             .WithName("GetUserDataExport")
             .WithSummary("Exporta todos os dados pessoais do usuário (LGPD art. 18 III).");

        group.MapPost("/{id:guid}/delete-request", RequestDeletion)
             .WithName("RequestUserDeletion")
             .WithSummary("Solicita exclusão de dados (soft-delete imediato, hard-delete em 30 dias).");

        group.MapDelete("/{id:guid}/delete-request", CancelDeletion)
             .WithName("CancelUserDeletion")
             .WithSummary("Admin cancela uma solicitação de exclusão pendente.")
             .RequireAuthorization(Policies.CanManageUsers);

        return endpoints;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> GetDataExport(
        Guid id,
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        if (!IsSelfOrAdmin(principal, id))
            return Results.Forbid();

        var result = await mediator.Send(new GetDataExportQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> RequestDeletion(
        Guid id,
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        if (!IsSelfOrAdmin(principal, id))
            return Results.Forbid();

        var requesterId = GetRequesterId(principal);
        var result = await mediator.Send(new RequestDeletionCommand(id, requesterId), ct);
        return result is null ? Results.NotFound() : Results.Accepted($"/api/users/{id}/delete-request", result);
    }

    private static async Task<IResult> CancelDeletion(
        Guid id,
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        var adminId = GetRequesterId(principal);
        var ok = await mediator.Send(new CancelDeletionCommand(id, adminId), ct);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsSelfOrAdmin(ClaimsPrincipal principal, Guid targetId)
    {
        if (principal.IsInRole("Admin")) return true;

        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Guid.TryParse(raw, out var selfId) && selfId == targetId;
    }

    private static Guid GetRequesterId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
