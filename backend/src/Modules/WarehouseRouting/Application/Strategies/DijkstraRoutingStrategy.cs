using IMS.Modular.Modules.WarehouseRouting.Domain.Models;

namespace IMS.Modular.Modules.WarehouseRouting.Application.Strategies;

/// <summary>
/// Dijkstra-based routing: finds exact shortest path between nodes.
/// Uses Nearest Neighbor heuristic for multi-stop TSP approximation.
/// Best for: small/medium warehouses where precision matters.
/// </summary>
public sealed class DijkstraRoutingStrategy : IRoutingStrategy
{
    public string Name => "Dijkstra";

    public List<WarehouseNode> CalculateRoute(
        WarehouseGraph graph,
        WarehouseNode start,
        IReadOnlyList<WarehouseNode> targets)
    {
        if (targets.Count == 0) return [start];
        if (targets.Count == 1) return FindShortestPath(graph, start, targets[0]);

        // Nearest Neighbor TSP heuristic using Dijkstra for each segment
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
                var path = FindShortestPath(graph, current, target);
                var dist = path.Count - 1; // path length = number of edges

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = target;
                    nearestPath = path;
                }
            }

            if (nearest is null) break;

            // Add path (excluding first node which is already in route)
            route.AddRange(nearestPath!.Skip(1));
            remaining.Remove(nearest);
            current = nearest;
        }

        return route;
    }

    /// <summary>
    /// Standard Dijkstra's algorithm for shortest path between two nodes.
    /// </summary>
    public static List<WarehouseNode> FindShortestPath(
        WarehouseGraph graph, WarehouseNode source, WarehouseNode destination)
    {
        var distances = new Dictionary<Guid, double>();
        var previous = new Dictionary<Guid, WarehouseNode?>();
        var pq = new PriorityQueue<WarehouseNode, double>();

        foreach (var node in graph.Nodes.Values)
        {
            distances[node.LocationId] = double.MaxValue;
            previous[node.LocationId] = null;
        }

        distances[source.LocationId] = 0;
        pq.Enqueue(source, 0);

        while (pq.TryDequeue(out var current, out var currentDist))
        {
            if (current.LocationId == destination.LocationId)
                break;

            if (currentDist > distances[current.LocationId])
                continue;

            foreach (var (neighbor, cost) in current.Edges)
            {
                var alt = distances[current.LocationId] + cost;
                if (alt < distances[neighbor.LocationId])
                {
                    distances[neighbor.LocationId] = alt;
                    previous[neighbor.LocationId] = current;
                    pq.Enqueue(neighbor, alt);
                }
            }
        }

        // Reconstruct path
        var path = new List<WarehouseNode>();
        var step = destination;
        while (step is not null)
        {
            path.Add(step);
            previous.TryGetValue(step.LocationId, out step);
        }

        path.Reverse();
        return path.Count > 0 && path[0].LocationId == source.LocationId ? path : [source];
    }
}
