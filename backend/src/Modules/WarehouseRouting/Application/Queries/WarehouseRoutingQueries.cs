using IMS.Modular.Modules.WarehouseRouting.Domain.Models;
using MediatR;

namespace IMS.Modular.Modules.WarehouseRouting.Application.Queries;

/// <summary>
/// Calculates the optimal route through warehouse locations for a stock-out operation.
/// </summary>
public record CalculateRouteQuery(
    /// <summary>List of location IDs to visit (pick locations).</summary>
    List<Guid> TargetLocationIds,
    /// <summary>Starting location (entry point). If null, uses default warehouse entry.</summary>
    Guid? StartLocationId = null,
    /// <summary>Algorithm preference: "Dijkstra" or "AStar". Null = auto-select.</summary>
    string? Strategy = null) : IRequest<RouteResult>;

/// <summary>
/// Returns the warehouse layout as a grid for visualization.
/// </summary>
public record GetWarehouseLayoutQuery(
    Guid? WarehouseId = null) : IRequest<WarehouseLayoutDto>;

public sealed class WarehouseLayoutDto
{
    public int Rows { get; init; }
    public int Cols { get; init; }
    public List<WarehouseNodeDto> Nodes { get; init; } = [];
}

public sealed class WarehouseNodeDto
{
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int Row { get; init; }
    public int Col { get; init; }
    public bool IsWalkable { get; init; }
    public string Type { get; init; } = string.Empty;
}
