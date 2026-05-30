using IMS.Modular.Modules.WarehouseRouting.Domain.Models;

namespace IMS.Modular.Modules.WarehouseRouting.Application.Strategies;

/// <summary>
/// A* routing: uses Manhattan distance heuristic for faster pathfinding.
/// Best for: large warehouses where Dijkstra is too slow.
/// </summary>
public sealed class AStarRoutingStrategy : IRoutingStrategy
{
    public string Name => "AStar";

    public List<WarehouseNode> CalculateRoute(
        WarehouseGraph graph,
        WarehouseNode start,
        IReadOnlyList<WarehouseNode> targets)
    {
        if (targets.Count == 0) return [start];
        if (targets.Count == 1) return FindPath(start, targets[0]);

        // Nearest Neighbor TSP with A* for each segment
        var route = new List<WarehouseNode> { start };
        var remaining = new HashSet<WarehouseNode>(targets);
        var current = start;

        while (remaining.Count > 0)
        {
            WarehouseNode? nearest = null;
            List<WarehouseNode>? nearestPath = null;
            double minDist = double.MaxValue;

            foreach (var target in remaining)
            {
                var path = FindPath(current, target);
                var dist = path.Count - 1;

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = target;
                    nearestPath = path;
                }
            }

            if (nearest is null) break;

            route.AddRange(nearestPath!.Skip(1));
            remaining.Remove(nearest);
            current = nearest;
        }

        return route;
    }

    /// <summary>
    /// A* pathfinding with Manhattan distance heuristic.
    /// </summary>
    private static List<WarehouseNode> FindPath(WarehouseNode source, WarehouseNode destination)
    {
        var openSet = new PriorityQueue<WarehouseNode, double>();
        var cameFrom = new Dictionary<Guid, WarehouseNode>();
        var gScore = new Dictionary<Guid, double> { [source.LocationId] = 0 };
        var fScore = new Dictionary<Guid, double> { [source.LocationId] = Heuristic(source, destination) };

        openSet.Enqueue(source, fScore[source.LocationId]);
        var inOpenSet = new HashSet<Guid> { source.LocationId };

        while (openSet.TryDequeue(out var current, out _))
        {
            inOpenSet.Remove(current.LocationId);

            if (current.LocationId == destination.LocationId)
                return ReconstructPath(cameFrom, current);

            foreach (var (neighbor, cost) in current.Edges)
            {
                var tentativeG = gScore.GetValueOrDefault(current.LocationId, double.MaxValue) + cost;

                if (tentativeG < gScore.GetValueOrDefault(neighbor.LocationId, double.MaxValue))
                {
                    cameFrom[neighbor.LocationId] = current;
                    gScore[neighbor.LocationId] = tentativeG;
                    fScore[neighbor.LocationId] = tentativeG + Heuristic(neighbor, destination);

                    if (!inOpenSet.Contains(neighbor.LocationId))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor.LocationId]);
                        inOpenSet.Add(neighbor.LocationId);
                    }
                }
            }
        }

        return [source]; // No path found
    }

    /// <summary>
    /// Manhattan distance heuristic for grid-based warehouse.
    /// </summary>
    private static double Heuristic(WarehouseNode a, WarehouseNode b)
        => Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col);

    private static List<WarehouseNode> ReconstructPath(Dictionary<Guid, WarehouseNode> cameFrom, WarehouseNode current)
    {
        var path = new List<WarehouseNode> { current };
        while (cameFrom.TryGetValue(current.LocationId, out var prev))
        {
            path.Add(prev);
            current = prev;
        }
        path.Reverse();
        return path;
    }
}
