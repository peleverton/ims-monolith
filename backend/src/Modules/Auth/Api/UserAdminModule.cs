using FluentValidation;
using IMS.Modular.Modules.Auth.Application.DTOs;
using IMS.Modular.Modules.Auth.Application.Services;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.Auth.Api;

/// <summary>
/// US-040 — Admin endpoints para gerenciamento de usuários.
/// Requer role Admin.
/// </summary>
public class UserAdminModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin/users")
            .WithTags("Admin - Users")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetUsers).WithName("AdminGetUsers");
        group.MapGet("/{id:guid}", GetUserById).WithName("AdminGetUserById");
        group.MapGet("/roles", GetRoles).WithName("AdminGetRoles");
        group.MapPatch("/{id:guid}/role", UpdateUserRole).WithName("AdminUpdateUserRole");
        group.MapPatch("/{id:guid}/activate", ActivateUser).WithName("AdminActivateUser");
        group.MapPatch("/{id:guid}/deactivate", DeactivateUser).WithName("AdminDeactivateUser");
        group.MapPost("/invite", InviteUser).WithName("AdminInviteUser");

        return endpoints;
    }

    private static async Task<IResult> GetUsers(
        IUserAdminService svc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
    {
        var result = await svc.GetUsersAsync(page, pageSize, search, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUserById(Guid id, IUserAdminService svc, CancellationToken ct)
    {
        var user = await svc.GetUserByIdAsync(id, ct);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> GetRoles(IUserAdminService svc, CancellationToken ct)
    {
        var roles = await svc.GetRolesAsync(ct);
        return Results.Ok(roles);
    }

    private static async Task<IResult> UpdateUserRole(
        Guid id,
        UpdateUserRoleRequest request,
        IValidator<UpdateUserRoleRequest> validator,
        IUserAdminService svc,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await svc.UpdateUserRoleAsync(id, request.RoleName, ct);
        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> ActivateUser(Guid id, IUserAdminService svc, CancellationToken ct)
    {
        var result = await svc.SetUserActiveAsync(id, true, ct);
        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> DeactivateUser(
        Guid id,
        ClaimsPrincipal principal,
        IUserAdminService svc,
        CancellationToken ct)
    {
        // Não pode desativar a si mesmo
        if (principal.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return Results.BadRequest(new { message = "Você não pode desativar sua própria conta." });

        var result = await svc.SetUserActiveAsync(id, false, ct);
        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> InviteUser(
        InviteUserRequest request,
        IValidator<InviteUserRequest> validator,
        IUserAdminService svc,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await svc.InviteUserAsync(request, ct);
        return result is null
            ? Results.BadRequest(new { message = "Username ou e-mail já existe." })
            : Results.Created($"/api/admin/users/{result.Id}", result);
    }
}
