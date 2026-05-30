using IMS.Modular.Modules.SmartAssigner.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IMS.Modular.Modules.SmartAssigner.Api;

public static class SmartAssignerModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/smart-assigner")
            .WithTags("SmartAssigner")
            .RequireAuthorization();

        // POST /api/smart-assigner/assign/{issueId} — trigger auto-assignment
        group.MapPost("/assign/{issueId:guid}", async (
            Guid issueId,
            bool? dryRun,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new TriggerSmartAssignCommand(issueId, dryRun ?? false));
            return Results.Ok(result);
        })
        .WithName("TriggerSmartAssign")
        .WithDescription("Manually triggers the Smart Assigner for a specific issue");

        // GET /api/smart-assigner/candidates — list all candidates with scores
        group.MapGet("/candidates", async (
            IMediator mediator) =>
        {
            // Use a dummy issue to show all candidates with their scores
            var result = await mediator.Send(new TriggerSmartAssignCommand(Guid.Empty, DryRun: true));
            return Results.Ok(result.Candidates);
        })
        .WithName("GetSmartAssignerCandidates")
        .WithDescription("Returns all candidates ranked by workload score");

        return endpoints;
    }
}
