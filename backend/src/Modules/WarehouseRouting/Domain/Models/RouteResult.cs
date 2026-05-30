namespace IMS.Modular.Modules.WarehouseRouting.Domain.Models;

/// <summary>
/// Result of a pathfinding calculation.
/// </summary>
public sealed class RouteResult
{
    public List<RouteStep> Steps { get; init; } = [];
    public double TotalDistance { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public TimeSpan ComputationTime { get; init; }

    /// <summary>
    /// Ordered list of location IDs in the optimal visit sequence.
    /// </summary>
    public List<Guid> VisitOrder => Steps.Select(s => s.LocationId).ToList();
}

/// <summary>
/// A single step in the route with navigation instruction.
/// </summary>
public sealed class RouteStep
{
    public int Order { get; init; }
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public int Row { get; init; }
    public int Col { get; init; }
    public string Instruction { get; init; } = string.Empty;
    public double DistanceFromPrevious { get; init; }
}
