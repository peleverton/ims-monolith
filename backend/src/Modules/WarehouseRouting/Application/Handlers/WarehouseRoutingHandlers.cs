using System.Diagnostics;
using IMS.Modular.Modules.WarehouseRouting.Application.Queries;
using IMS.Modular.Modules.WarehouseRouting.Application.Strategies;
using IMS.Modular.Modules.WarehouseRouting.Domain.Models;
using IMS.Modular.Modules.WarehouseRouting.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMS.Modular.Modules.WarehouseRouting.Application.Handlers;

public sealed class CalculateRouteHandler(
    IWarehouseGraphRepository graphRepository,
    IEnumerable<IRoutingStrategy> strategies,
    ILogger<CalculateRouteHandler> logger) : IRequestHandler<CalculateRouteQuery, RouteResult>
{
    private const int LargeWarehouseThreshold = 100;

    public async Task<RouteResult> Handle(CalculateRouteQuery request, CancellationToken ct)
    {
        var graph = await graphRepository.BuildGraphAsync(ct: ct);

        if (graph.Nodes.Count == 0)
        {
            logger.LogWarning("[WarehouseRouting] No walkable nodes in warehouse graph");
            return new RouteResult { Strategy = "None", Steps = [] };
        }

        // Resolve start node
        var start = request.StartLocationId.HasValue && graph.Nodes.ContainsKey(request.StartLocationId.Value)
            ? graph.Nodes[request.StartLocationId.Value]
            : graph.EntryPoint ?? graph.Nodes.Values.First();

        // Resolve target nodes
        var targets = request.TargetLocationIds
            .Where(id => graph.Nodes.ContainsKey(id))
            .Select(id => graph.Nodes[id])
            .ToList();

        if (targets.Count == 0)
        {
            logger.LogWarning("[WarehouseRouting] None of the target locations found in graph");
            return new RouteResult { Strategy = "None", Steps = [] };
        }

        // Strategy selection: explicit choice or auto-select based on graph size
        var strategy = SelectStrategy(request.Strategy, graph.Nodes.Count);

        logger.LogInformation(
            "[WarehouseRouting] Using {Strategy} for {NodeCount} nodes, {TargetCount} targets",
            strategy.Name, graph.Nodes.Count, targets.Count);

        // Execute pathfinding
        var sw = Stopwatch.StartNew();
        var path = strategy.CalculateRoute(graph, start, targets);
        sw.Stop();

        // Build navigation instructions
        var result = RouteInstructionBuilder.Build(path, strategy.Name, sw.Elapsed);

        logger.LogInformation(
            "[WarehouseRouting] Route calculated: {Steps} steps, {Distance} total distance, {Time}ms",
            result.Steps.Count, result.TotalDistance, sw.ElapsedMilliseconds);

        return result;
    }

    private IRoutingStrategy SelectStrategy(string? requested, int nodeCount)
    {
        if (!string.IsNullOrEmpty(requested))
        {
            var match = strategies.FirstOrDefault(s =>
                s.Name.Equals(requested, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }

        // Auto-select: A* for large warehouses, Dijkstra for small
        return nodeCount > LargeWarehouseThreshold
            ? strategies.First(s => s is AStarRoutingStrategy)
            : strategies.First(s => s is DijkstraRoutingStrategy);
    }
}

public sealed class GetWarehouseLayoutHandler(
    IWarehouseGraphRepository graphRepository)
    : IRequestHandler<GetWarehouseLayoutQuery, WarehouseLayoutDto>
{
    public async Task<WarehouseLayoutDto> Handle(GetWarehouseLayoutQuery request, CancellationToken ct)
    {
        var graph = await graphRepository.BuildGraphAsync(request.WarehouseId, ct);

        return new WarehouseLayoutDto
        {
            Rows = graph.Rows,
            Cols = graph.Cols,
            Nodes = graph.Nodes.Values.Select(n => new WarehouseNodeDto
            {
                LocationId = n.LocationId,
                Name = n.Name,
                Code = n.Code,
                Row = n.Row,
                Col = n.Col,
                IsWalkable = n.IsWalkable,
                Type = "Location"
            }).ToList()
        };
    }
}
