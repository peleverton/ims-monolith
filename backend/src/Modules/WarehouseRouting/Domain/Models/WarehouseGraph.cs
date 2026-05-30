namespace IMS.Modular.Modules.WarehouseRouting.Domain.Models;

/// <summary>
/// Represents a node in the warehouse graph (a Location).
/// Each node has grid coordinates for pathfinding.
/// </summary>
public sealed class WarehouseNode
{
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int Row { get; init; }
    public int Col { get; init; }
    public bool IsWalkable { get; init; } = true;

    /// <summary>
    /// Adjacent nodes with their traversal cost (distance).
    /// </summary>
    public List<(WarehouseNode Neighbor, double Cost)> Edges { get; } = [];

    public override int GetHashCode() => LocationId.GetHashCode();
    public override bool Equals(object? obj) => obj is WarehouseNode other && LocationId == other.LocationId;
}

/// <summary>
/// Represents the warehouse as a graph of walkable nodes.
/// Built from Location hierarchy (Warehouse → Aisle → Shelf).
/// </summary>
public sealed class WarehouseGraph
{
    public int Rows { get; init; }
    public int Cols { get; init; }
    public Dictionary<Guid, WarehouseNode> Nodes { get; } = [];
    public WarehouseNode? EntryPoint { get; set; }

    /// <summary>
    /// Builds a grid-based graph from a list of locations with row/col metadata.
    /// Adjacent cells (4-directional) are connected with cost 1.0.
    /// </summary>
    public static WarehouseGraph BuildFromGrid(IReadOnlyList<WarehouseNodeInput> inputs, int rows, int cols)
    {
        var graph = new WarehouseGraph { Rows = rows, Cols = cols };

        // Create nodes
        foreach (var input in inputs)
        {
            var node = new WarehouseNode
            {
                LocationId = input.LocationId,
                Name = input.Name,
                Code = input.Code,
                Row = input.Row,
                Col = input.Col,
                IsWalkable = input.IsWalkable
            };
            graph.Nodes[input.LocationId] = node;
        }

        // Build adjacency (4-directional grid connectivity)
        var grid = new WarehouseNode?[rows, cols];
        foreach (var node in graph.Nodes.Values)
        {
            if (node.Row < rows && node.Col < cols)
                grid[node.Row, node.Col] = node;
        }

        int[] dr = [-1, 1, 0, 0];
        int[] dc = [0, 0, -1, 1];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var current = grid[r, c];
                if (current is null || !current.IsWalkable) continue;

                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dr[d], nc = c + dc[d];
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;

                    var neighbor = grid[nr, nc];
                    if (neighbor is not null && neighbor.IsWalkable)
                    {
                        current.Edges.Add((neighbor, 1.0));
                    }
                }
            }
        }

        // First node is the entry point by default
        graph.EntryPoint = graph.Nodes.Values.FirstOrDefault(n => n.Row == 0 && n.Col == 0)
                           ?? graph.Nodes.Values.FirstOrDefault();

        return graph;
    }
}

/// <summary>
/// Input data for building the warehouse graph.
/// </summary>
public sealed class WarehouseNodeInput
{
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int Row { get; init; }
    public int Col { get; init; }
    public bool IsWalkable { get; init; } = true;
}
