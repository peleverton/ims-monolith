using FluentValidation;
using IMS.Modular.Modules.Issues.Application.Commands;
using IMS.Modular.Modules.Issues.Application.DTOs;
using IMS.Modular.Modules.Issues.Application.Queries;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Modular.Modules.Issues.Api;

public class IssuesModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/issues")
            .WithTags("Issues")
            .RequireAuthorization(Policies.CanCreateIssue);

        group.MapGet("/", GetAll).WithName("GetAllIssues");
        group.MapGet("/{id:guid}", GetById).WithName("GetIssueById");
        group.MapGet("/status/{status}", GetByStatus).WithName("GetIssuesByStatus");
        group.MapGet("/priority/{priority}", GetByPriority).WithName("GetIssuesByPriority");
        group.MapGet("/search", Search).WithName("SearchIssues");
        group.MapGet("/user/{userId:guid}", GetUserIssues).WithName("GetUserIssues");
        group.MapPost("/", Create).WithName("CreateIssue");
        group.MapPut("/{id:guid}", Update).WithName("UpdateIssue");
        group.MapPatch("/{id:guid}/status", UpdateStatus).WithName("UpdateIssueStatus");
        group.MapPatch("/{id:guid}/resolve", Resolve).WithName("ResolveIssue");
        group.MapPatch("/{id:guid}/assign", Assign).WithName("AssignIssue");
        group.MapPost("/{id:guid}/comments", AddComment).WithName("AddComment");
        group.MapPost("/{id:guid}/tags", AddTag).WithName("AddTag");
        // Delete requires Manager or Admin (US-057)
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteIssue")
             .RequireAuthorization(Policies.CanManageIssues);

        return endpoints;
    }

    private static async Task<IResult> GetAll(
        IMediator mediator,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        IssueStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<IssueStatus>(status, true, out var ps))
            statusEnum = ps;

        IssuePriority? priorityEnum = null;
        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<IssuePriority>(priority, true, out var pp))
            priorityEnum = pp;

        var query = new GetAllIssuesQuery(pageNumber, pageSize, sortBy, sortDirection, statusEnum, priorityEnum, searchTerm);
        return Results.Ok(await mediator.Send(query, ct));
    }

    private static async Task<IResult> GetById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetIssueByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetByStatus(string status, IMediator mediator,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        if (!Enum.TryParse<IssueStatus>(status, true, out var statusEnum))
            return Results.BadRequest(new { error = "Invalid status value" });

        return Results.Ok(await mediator.Send(new GetIssuesByStatusQuery(statusEnum, pageNumber, pageSize), ct));
    }

    private static async Task<IResult> GetByPriority(string priority, IMediator mediator,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        if (!Enum.TryParse<IssuePriority>(priority, true, out var priorityEnum))
            return Results.BadRequest(new { error = "Invalid priority value" });

        return Results.Ok(await mediator.Send(new GetIssuesByPriorityQuery(priorityEnum, pageNumber, pageSize), ct));
    }

    private static async Task<IResult> Search(IMediator mediator,
        [FromQuery] string searchTerm, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Results.BadRequest(new { error = "Search term is required" });

        return Results.Ok(await mediator.Send(new SearchIssuesQuery(searchTerm, pageNumber, pageSize), ct));
    }

    private static async Task<IResult> GetUserIssues(Guid userId, IMediator mediator,
        [FromQuery] bool asAssignee = true, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return Results.Ok(await mediator.Send(new GetUserIssuesQuery(userId, asAssignee, pageNumber, pageSize), ct));
    }

    private static async Task<IResult> Create(
        CreateIssueRequest request,
        IValidator<CreateIssueRequest> validator,
        IMediator mediator,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var userId = GetUserId(user);
        var command = new CreateIssueCommand(request.Title, request.Description, request.Priority, userId, request.DueDate);
        var result = await mediator.Send(command, ct);

        return result.ToCreatedResult($"/api/issues/{result.Value?.Id}", httpContext);
    }

    private static async Task<IResult> Update(
        Guid id, UpdateIssueRequest request,
        IValidator<UpdateIssueRequest> validator,
        IMediator mediator, ClaimsPrincipal user,
        HttpContext httpContext, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var userId = GetUserId(user);
        var result = await mediator.Send(new UpdateIssueCommand(id, request.Title, request.Description, request.Priority, request.DueDate, userId), ct);

        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> UpdateStatus(
        Guid id, UpdateStatusRequest request,
        IValidator<UpdateStatusRequest> validator,
        IMediator mediator, ClaimsPrincipal user,
        HttpContext httpContext, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        if (!Enum.TryParse<IssueStatus>(request.Status, true, out var status))
            return Results.BadRequest(new { error = "Invalid status value" });

        var userId = GetUserId(user);
        var result = await mediator.Send(new ChangeIssueStatusCommand(id, status, userId), ct);

        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> Resolve(
        Guid id, ResolveIssueRequest request,
        IMediator mediator, ClaimsPrincipal user,
        HttpContext httpContext, CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new ChangeIssueStatusCommand(id, IssueStatus.Resolved, userId), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> Assign(
        Guid id, AssignIssueRequest request,
        IValidator<AssignIssueRequest> validator,
        IMediator mediator, ClaimsPrincipal user,
        HttpContext httpContext, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var userId = GetUserId(user);
        var result = await mediator.Send(new AssignIssueCommand(id, request.AssigneeId, userId), ct);

        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> AddComment(
        Guid id, AddCommentRequest request,
        IValidator<AddCommentRequest> validator,
        IMediator mediator, ClaimsPrincipal user,
        HttpContext httpContext, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var userId = GetUserId(user);
        var result = await mediator.Send(new AddCommentCommand(id, request.Content, userId), ct);

        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> AddTag(
        Guid id, AddTagRequest request,
        IValidator<AddTagRequest> validator,
        IMediator mediator, ClaimsPrincipal user,
        HttpContext httpContext, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var userId = GetUserId(user);
        var result = await mediator.Send(new AddTagCommand(id, request.Name, request.Color, userId), ct);

        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteIssueCommand(id), ct);
        return result.ToApiResult(httpContext);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
