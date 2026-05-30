using IMS.Modular.Modules.WarehouseRouting.Application.Queries;
using MediatR;

namespace IMS.Modular.Modules.WarehouseRouting.Api;

public static class WarehouseRoutingModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/warehouse-routing")
            .WithTags("WarehouseRouting")
            .RequireAuthorization();

        // POST /api/warehouse-routing/calculate — calculate optimal pickup route
        group.MapPost("/calculate", async (CalculateRouteRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new CalculateRouteQuery(
                request.TargetLocationIds,
                request.StartLocationId,
                request.Strategy));
            return Results.Ok(result);
        })
        .WithName("CalculateWarehouseRoute")
        .WithDescription("Calculates the optimal route to visit all target locations in a warehouse");

        // GET /api/warehouse-routing/layout — get warehouse grid layout for visualization
        group.MapGet("/layout", async (Guid? warehouseId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWarehouseLayoutQuery(warehouseId));
            return Results.Ok(result);
        })
        .WithName("GetWarehouseLayout")
        .WithDescription("Returns the warehouse layout as a grid for map visualization");

        return endpoints;
    }
}

public sealed record CalculateRouteRequest(
    List<Guid> TargetLocationIds,
    Guid? StartLocationId = null,
    string? Strategy = null);
