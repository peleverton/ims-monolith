using IMS.Modular.Modules.InventoryIssues.Application.Commands;
using IMS.Modular.Modules.InventoryIssues.Application.DTOs;
using IMS.Modular.Modules.InventoryIssues.Application.Queries;
using IMS.Modular.Modules.InventoryIssues.Domain.Enums;
using IMS.Modular.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.InventoryIssues.Api;

public class InventoryIssuesModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/inventory-issues")
            .WithTags("Inventory Issues")
            .RequireAuthorization();

        // List / search
        group.MapGet("/", GetAll).WithName("GetInventoryIssues");
        group.MapGet("/{id:guid}", GetById).WithName("GetInventoryIssueById");
        group.MapGet("/overdue", GetOverdue).WithName("GetOverdueInventoryIssues");
        group.MapGet("/high-priority", GetHighPriority).WithName("GetHighPriorityInventoryIssues");
        group.MapGet("/statistics", GetStatistics).WithName("GetInventoryIssueStatistics");

        // CRUD
        group.MapPost("/", Create).WithName("CreateInventoryIssue");
        group.MapPut("/{id:guid}", Update).WithName("UpdateInventoryIssue");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteInventoryIssue");

        // Lifecycle
        group.MapPatch("/{id:guid}/assign", Assign).WithName("AssignInventoryIssue");
        group.MapPatch("/{id:guid}/resolve", Resolve).WithName("ResolveInventoryIssue");
        group.MapPatch("/{id:guid}/close", Close).WithName("CloseInventoryIssue");
        group.MapPatch("/{id:guid}/reopen", Reopen).WithName("ReopenInventoryIssue");

        return endpoints;
    }

    private static async Task<IResult> GetAll(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? type = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? reporterId = null,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        Enum.TryParse<InventoryIssueStatus>(status, true, out var statusEnum);
        Enum.TryParse<InventoryIssuePriority>(priority, true, out var priorityEnum);
        Enum.TryParse<InventoryIssueType>(type, true, out var typeEnum);

        var result = await mediator.Send(new GetInventoryIssuesQuery(
            page, pageSize,
            string.IsNullOrEmpty(status) ? null : statusEnum,
            string.IsNullOrEmpty(priority) ? null : priorityEnum,
            string.IsNullOrEmpty(type) ? null : typeEnum,
            productId, locationId, reporterId, assigneeId, search), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var issue = await mediator.Send(new GetInventoryIssueByIdQuery(id), ct);
        return issue is null ? Results.NotFound() : Results.Ok(issue);
    }

    private static async Task<IResult> GetOverdue(
        IMediator mediator, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetOverdueInventoryIssuesQuery(page, pageSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetHighPriority(
        IMediator mediator, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetHighPriorityInventoryIssuesQuery(page, pageSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetStatistics(IMediator mediator, CancellationToken ct)
    {
        var stats = await mediator.Send(new GetInventoryIssueStatisticsQuery(), ct);
        return Results.Ok(stats);
    }

    private static async Task<IResult> Create(
        CreateInventoryIssueRequest req, IMediator mediator, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateInventoryIssueCommand(
            req.Title, req.Description, req.Type, req.Priority, req.ReporterId,
            req.ProductId, req.LocationId, req.AffectedQuantity, req.EstimatedLoss, req.DueDate), ct);
        return Results.Created($"/api/inventory-issues/{id}", new { id });
    }

    private static async Task<IResult> Update(
        Guid id, UpdateInventoryIssueRequest req, IMediator mediator, CancellationToken ct)
    {
        var ok = await mediator.Send(new UpdateInventoryIssueCommand(
            id, req.Title, req.Description, req.Type, req.Priority,
            req.ProductId, req.LocationId, req.AffectedQuantity, req.EstimatedLoss, req.DueDate), ct);
        return ok ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var ok = await mediator.Send(new DeleteInventoryIssueCommand(id), ct);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> Assign(
        Guid id, AssignInventoryIssueRequest req, IMediator mediator,
        IUserContext user, CancellationToken ct)
    {
        var ok = await mediator.Send(new AssignInventoryIssueCommand(id, req.AssigneeId, user.UserId.GetValueOrDefault()), ct);
        return ok ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> Resolve(
        Guid id, ResolveInventoryIssueRequest req, IMediator mediator,
        IUserContext user, CancellationToken ct)
    {
        var ok = await mediator.Send(new ResolveInventoryIssueCommand(id, req.ResolutionNotes, user.UserId.GetValueOrDefault()), ct);
        return ok ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> Close(Guid id, IMediator mediator, IUserContext user, CancellationToken ct)
    {
        var ok = await mediator.Send(new CloseInventoryIssueCommand(id, user.UserId.GetValueOrDefault()), ct);
        return ok ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> Reopen(Guid id, IMediator mediator, IUserContext user, CancellationToken ct)
    {
        var ok = await mediator.Send(new ReopenInventoryIssueCommand(id, user.UserId.GetValueOrDefault()), ct);
        return ok ? Results.Ok() : Results.NotFound();
    }
}
