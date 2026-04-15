using IMS.Modular.Modules.UserManagement.Application.Commands;
using IMS.Modular.Modules.UserManagement.Application.Queries;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.UserManagement.Api;

public class UserManagementModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/users")
            .WithTags("User Management")
            .RequireAuthorization();

        // ── US-019: Queries ──────────────────────────────────────────────────

        // GET /api/users  (Admin only — paginated + search)
        group.MapGet("/", GetPagedUsers)
            .WithName("GetPagedUsers")
            .RequireAuthorization("Admin");

        // GET /api/users/me  (current user profile)
        group.MapGet("/me", GetCurrentUserProfile)
            .WithName("GetCurrentUserProfile");

        // GET /api/users/active  (Admin only)
        group.MapGet("/active", GetActiveUsers)
            .WithName("GetActiveUsers")
            .RequireAuthorization("Admin");

        // GET /api/users/role/{roleId}  (Admin only)
        group.MapGet("/role/{roleId:guid}", GetUsersByRole)
            .WithName("GetUsersByRole")
            .RequireAuthorization("Admin");

        // GET /api/users/{id}
        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById");

        // ── US-019: Commands ─────────────────────────────────────────────────

        // PUT /api/users/{id}  (self or Admin)
        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("UpdateUser");

        // PUT /api/users/{id}/profile
        group.MapPut("/{id:guid}/profile", UpdateUserProfile)
            .WithName("UpdateUserProfile");

        // PUT /api/users/{id}/password
        group.MapPut("/{id:guid}/password", ChangePassword)
            .WithName("ChangePassword");

        // PATCH /api/users/{id}/activate  (Admin only)
        group.MapPatch("/{id:guid}/activate", ActivateUser)
            .WithName("ActivateUser")
            .RequireAuthorization("Admin");

        // PATCH /api/users/{id}/deactivate  (Admin only)
        group.MapPatch("/{id:guid}/deactivate", DeactivateUser)
            .WithName("DeactivateUser")
            .RequireAuthorization("Admin");

        // POST /api/users/{id}/roles/{roleId}  (Admin only)
        group.MapPost("/{id:guid}/roles/{roleId:guid}", AssignRole)
            .WithName("AssignRole")
            .RequireAuthorization("Admin");

        // DELETE /api/users/{id}/roles/{roleId}  (Admin only)
        group.MapDelete("/{id:guid}/roles/{roleId:guid}", RemoveRole)
            .WithName("RemoveRole")
            .RequireAuthorization("Admin");

        // DELETE /api/users/{id}  (Admin only)
        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .RequireAuthorization("Admin");

        return endpoints;
    }

    // ── Query handlers ────────────────────────────────────────────────────────

    private static async Task<IResult> GetPagedUsers(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Results.Ok(await mediator.Send(new GetPagedUsersQuery(page, pageSize, search), ct));

    private static async Task<IResult> GetCurrentUserProfile(
        IMediator mediator,
        IUserContext userContext,
        CancellationToken ct)
    {
        if (!userContext.IsAuthenticated || userContext.UserId is null)
            return Results.Unauthorized();

        var userId = userContext.UserId.Value;

        var result = await mediator.Send(new GetCurrentUserProfileQuery(userId), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetActiveUsers(IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetActiveUsersQuery(), ct));

    private static async Task<IResult> GetUsersByRole(
        Guid roleId, IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetUsersByRoleQuery(roleId), ct));

    private static async Task<IResult> GetUserById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // ── Command handlers ──────────────────────────────────────────────────────

    private static async Task<IResult> UpdateUser(
        Guid id, [FromBody] UpdateUserRequest req,
        IMediator mediator, CancellationToken ct)
    {
        var command = new UpdateUserCommand(
            id, req.FullName, req.Department, req.JobTitle,
            req.PhoneNumber, req.AvatarUrl, req.Bio, req.TimeZone);

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result);
    }

    private static async Task<IResult> UpdateUserProfile(
        Guid id, [FromBody] UpdateUserRequest req,
        IMediator mediator, CancellationToken ct)
    {
        var command = new UpdateUserProfileCommand(
            id, req.FullName, req.Department, req.JobTitle,
            req.PhoneNumber, req.AvatarUrl, req.Bio, req.TimeZone);

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result);
    }

    private static async Task<IResult> ChangePassword(
        Guid id, [FromBody] ChangePasswordRequest req,
        IMediator mediator, CancellationToken ct)
    {
        var command = new ChangePasswordCommand(id, req.CurrentPassword, req.NewPassword, req.ConfirmNewPassword);
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Results.NoContent() : MapError(result);
    }

    private static async Task<IResult> ActivateUser(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ActivateUserCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : MapError(result);
    }

    private static async Task<IResult> DeactivateUser(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivateUserCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : MapError(result);
    }

    private static async Task<IResult> AssignRole(
        Guid id, Guid roleId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new AssignRoleCommand(id, roleId), ct);
        return result.IsSuccess ? Results.NoContent() : MapError(result);
    }

    private static async Task<IResult> RemoveRole(
        Guid id, Guid roleId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveRoleCommand(id, roleId), ct);
        return result.IsSuccess ? Results.NoContent() : MapError(result);
    }

    private static async Task<IResult> DeleteUser(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteUserCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : MapError(result);
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    private static IResult MapError<T>(Shared.Domain.Result<T> result) =>
        result.ErrorCode switch
        {
            404 => Results.NotFound(new { error = result.Error }),
            409 => Results.Conflict(new { error = result.Error }),
            401 => Results.Unauthorized(),
            403 => Results.Forbid(),
            _ => Results.BadRequest(new { error = result.Error })
        };
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record UpdateUserRequest(
    string FullName,
    string? Department,
    string? JobTitle,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? TimeZone);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
