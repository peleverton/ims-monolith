using IMS.Modular.Modules.Jobs;
using IMS.Modular.Modules.Search.Application;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.Search.Api;

/// <summary>
/// US-071: GET /api/search?q=...&modules=issues,inventory&page=1&pageSize=20
/// US-088: POST /api/search/reindex — Admin-only full reindex trigger
/// </summary>
public static class SearchModule
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/search", async (
            [FromQuery] string q,
            [FromQuery] string? modules,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            ISearchService? searchService = null) =>
        {
            if (searchService is null)
                return Results.Problem("Search service is not available.", statusCode: 503);

            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "Query parameter 'q' is required." });

            var moduleList = modules?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();

            var response = await searchService.SearchAsync(q, moduleList, page, pageSize);
            return Results.Ok(response);
        })
        .WithName("Search")
        .WithTags("Search")
        .RequireAuthorization()
        .Produces<Domain.SearchResponse>(200)
        .Produces(400)
        .Produces(503);

        // US-088: Manual full reindex endpoint (Admin only)
        app.MapPost("/api/search/reindex", async (
            MeilisearchReindexJob? reindexJob,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Search.Reindex");
            if (reindexJob is null)
                return Results.Problem("Reindex service is not available.", statusCode: 503);

            // Fire-and-forget: enqueue via Task to avoid blocking the response
            _ = Task.Run(async () =>
            {
                try { await reindexJob.ExecuteAsync(fullReindex: true); }
                catch (Exception ex) { logger.LogError(ex, "[Reindex] Full reindex failed"); }
            });

            return Results.Accepted(value: new { message = "Full reindex triggered." });
        })
        .WithName("TriggerReindex")
        .WithTags("Search")
        .RequireAuthorization(IMS.Modular.Shared.Abstractions.Policies.CanManageUsers)
        .Produces(202)
        .Produces(503);
    }
}
