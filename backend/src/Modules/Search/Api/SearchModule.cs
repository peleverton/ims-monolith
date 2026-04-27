using IMS.Modular.Modules.Search.Application;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.Search.Api;

/// <summary>
/// US-071: GET /api/search?q=...&modules=issues,inventory&page=1&pageSize=20
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
    }
}
