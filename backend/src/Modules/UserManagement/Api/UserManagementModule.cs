using FluentValidation;
using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Application.DTOs;
using IMS.Modular.Modules.UserManagement.Application.Queries;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.UserManagement.Api;

/// <summary>
/// US-064: UserManagement Module — dedicated endpoints at /api/users.
/// Replaces the deprecated /api/admin/users from the Auth module.
/// </summary>
public class UserManagementModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/users")
            .WithTags("User Management")
            .RequireAuthorization(Policies.CanManageUsers);

        // Read
        group.MapGet("/",             GetUsers)       .WithName("GetUsers");
        group.MapGet("/{id:guid}",    GetUserById)    .WithName("GetUserById");
        group.MapGet("/roles",        GetRoles)       .WithName("GetUserRoles");

        // Write
        group.MapPost("/invite",                      InviteUser)       .WithName("InviteUser");
        group.MapPut("/{id:guid}/profile",            UpdateProfile)    .WithName("UpdateUserProfile");
        group.MapPatch("/{id:guid}/role",             ChangeRole)       .WithName("ChangeUserRole");
        group.MapPatch("/{id:guid}/activate",         ActivateUser)     .WithName("ActivateUser");
        group.MapPatch("/{id:guid}/deactivate",       DeactivateUser)   .WithName("DeactivateUser");

        return endpoints;
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetUsers(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(page, pageSize, search, role), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUserById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserByIdQuery(id), ct);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> GetRoles(IMediator mediator, CancellationToken ct)
    {
        var roles = await mediator.Send(new GetRolesQuery(), ct);
        return Results.Ok(roles);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    private static async Task<IResult> InviteUser(
        InviteUserRequest request,
        IValidator<InviteUserRequest> validator,
        IMediator mediator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await mediator.Send(
            new InviteUserCommand(request.Username, request.Email, request.FullName,
                                  request.Role, request.TemporaryPassword), ct);

        return result is null
            ? Results.Conflict(new { message = "Username ou e-mail já existe." })
            : Results.Created($"/api/users/{result.Id}", result);
    }

    private static async Task<IResult> UpdateProfile(
        Guid id,
        UpdateProfileRequest request,
        IValidator<UpdateProfileRequest> validator,
        IMediator mediator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await mediator.Send(new UpdateProfileCommand(id, request.FullName, request.Email), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        IValidator<ChangeUserRoleRequest> validator,
        IMediator mediator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var ok = await mediator.Send(new ChangeUserRoleCommand(id, request.RoleName), ct);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ActivateUser(
        Guid id, ClaimsPrincipal principal, IMediator mediator, CancellationToken ct)
    {
        var requesterId = GetRequesterId(principal);
        var ok = await mediator.Send(new SetUserActiveCommand(id, true, requesterId), ct);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> DeactivateUser(
        Guid id, ClaimsPrincipal principal, IMediator mediator, CancellationToken ct)
    {
        var requesterId = GetRequesterId(principal);
        if (requesterId == id)
            return Results.BadRequest(new { message = "Você não pode desativar sua própria conta." });

        var ok = await mediator.Send(new SetUserActiveCommand(id, false, requesterId), ct);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Guid GetRequesterId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
