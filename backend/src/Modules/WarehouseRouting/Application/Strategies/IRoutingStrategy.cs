using IMS.Modular.Modules.WarehouseRouting.Domain.Models;

namespace IMS.Modular.Modules.WarehouseRouting.Application.Strategies;

/// <summary>
/// Strategy Pattern: interface for different routing algorithms.
/// </summary>
public interface IRoutingStrategy
{
    string Name { get; }

    /// <summary>
    /// Calculates the shortest path visiting all target nodes starting from the entry point.
    /// </summary>
    List<WarehouseNode> CalculateRoute(
        WarehouseGraph graph,
        WarehouseNode start,
        IReadOnlyList<WarehouseNode> targets);
}
