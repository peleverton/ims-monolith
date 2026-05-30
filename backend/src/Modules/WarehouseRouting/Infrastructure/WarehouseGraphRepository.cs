using System.Data;
using Dapper;
using IMS.Modular.Modules.WarehouseRouting.Domain.Models;

namespace IMS.Modular.Modules.WarehouseRouting.Infrastructure;

/// <summary>
/// Reads warehouse location data and builds the graph for pathfinding.
/// Assigns grid coordinates based on location hierarchy and creation order.
/// </summary>
public interface IWarehouseGraphRepository
{
    Task<WarehouseGraph> BuildGraphAsync(Guid? warehouseId = null, CancellationToken ct = default);
}

public sealed class WarehouseGraphRepository(IDbConnection connection) : IWarehouseGraphRepository
{
    public async Task<WarehouseGraph> BuildGraphAsync(Guid? warehouseId = null, CancellationToken ct = default)
    {
        // Get all active locations of type Aisle/Shelf (walkable areas in a warehouse)
        const string sql = """
            SELECT
                l."Id" AS "LocationId",
                l."Name",
                l."Code",
                l."Type",
                l."ParentLocationId",
                l."IsActive"
            FROM "Locations" l
            WHERE l."IsActive" = true
              AND l."Type" IN ('Aisle', 'Shelf', 'Warehouse')
            ORDER BY l."Type" DESC, l."Code" ASC
            """;

        var locations = (await connection.QueryAsync<LocationRow>(sql)).ToList();

        if (locations.Count == 0)
            return new WarehouseGraph { Rows = 0, Cols = 0 };

        // Auto-assign grid coordinates based on location order
        // Aisles get rows, shelves within an aisle get columns
        var aisles = locations.Where(l => l.Type == "Aisle").OrderBy(l => l.Code).ToList();
        var shelves = locations.Where(l => l.Type == "Shelf").ToList();

        // Calculate grid dimensions
        int rows = Math.Max(aisles.Count, 1);
        // Max shelves per aisle determines columns (+ 1 for the aisle corridor)
        int maxShelvesPerAisle = aisles.Count > 0
            ? aisles.Max(a => shelves.Count(s => s.ParentLocationId == a.LocationId))
            : shelves.Count;
        int cols = Math.Max(maxShelvesPerAisle + 1, 2);

        var inputs = new List<WarehouseNodeInput>();

        // Assign coordinates: each aisle is a row, shelves are columns within that row
        for (int r = 0; r < aisles.Count; r++)
        {
            var aisle = aisles[r];

            // Aisle corridor at column 0
            inputs.Add(new WarehouseNodeInput
            {
                LocationId = aisle.LocationId,
                Name = aisle.Name,
                Code = aisle.Code,
                Row = r,
                Col = 0,
                IsWalkable = true
            });

            // Shelves in this aisle
            var aisleChildren = shelves
                .Where(s => s.ParentLocationId == aisle.LocationId)
                .OrderBy(s => s.Code)
                .ToList();

            for (int c = 0; c < aisleChildren.Count; c++)
            {
                var shelf = aisleChildren[c];
                inputs.Add(new WarehouseNodeInput
                {
                    LocationId = shelf.LocationId,
                    Name = shelf.Name,
                    Code = shelf.Code,
                    Row = r,
                    Col = c + 1, // offset by 1 for the corridor
                    IsWalkable = true
                });
            }
        }

        // If no aisles but we have shelves, distribute them in a grid
        if (aisles.Count == 0 && shelves.Count > 0)
        {
            cols = (int)Math.Ceiling(Math.Sqrt(shelves.Count));
            rows = (int)Math.Ceiling((double)shelves.Count / cols);

            for (int i = 0; i < shelves.Count; i++)
            {
                inputs.Add(new WarehouseNodeInput
                {
                    LocationId = shelves[i].LocationId,
                    Name = shelves[i].Name,
                    Code = shelves[i].Code,
                    Row = i / cols,
                    Col = i % cols,
                    IsWalkable = true
                });
            }
        }

        return WarehouseGraph.BuildFromGrid(inputs, rows, cols);
    }

    private sealed class LocationRow
    {
        public Guid LocationId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Guid? ParentLocationId { get; init; }
        public bool IsActive { get; init; }
    }
}
