namespace BlazorModules.Models;

// ─── Location Tree (Epic 7: Composite Pattern) ───────────────────────────────

/// <summary>
/// Flat DTO from API — matches backend LocationDto.
/// </summary>
public record LocationDto(
    Guid Id,
    string Name,
    string Code,
    string Type,
    int Capacity,
    string? Description,
    Guid? ParentLocationId,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Composite Pattern: tree node that can contain children (recursive).
/// Used by MudTreeView for hierarchical display.
/// </summary>
public class LocationTreeNode
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsExpanded { get; set; }

    /// <summary>Child nodes (Composite Pattern — same type as parent).</summary>
    public List<LocationTreeNode> Children { get; } = [];

    /// <summary>Whether this is a leaf node (no children = shelf/endpoint).</summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>Icon based on location type.</summary>
    public string Icon => Type switch
    {
        "Warehouse" => "🏭",
        "Aisle" => "🚶",
        "Shelf" => "📦",
        "Store" => "🏪",
        "DistributionCenter" => "🏗️",
        _ => "📍"
    };

    /// <summary>
    /// Transforms a flat list of LocationDto into a tree structure.
    /// Algorithm: O(N) using a dictionary for parent lookup.
    /// </summary>
    public static List<LocationTreeNode> BuildTree(IReadOnlyList<LocationDto> flatList)
    {
        var nodeMap = new Dictionary<Guid, LocationTreeNode>();
        var roots = new List<LocationTreeNode>();

        // Pass 1: Create all nodes
        foreach (var dto in flatList)
        {
            nodeMap[dto.Id] = new LocationTreeNode
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                Type = dto.Type,
                Capacity = dto.Capacity,
                Description = dto.Description,
                IsActive = dto.IsActive,
                IsExpanded = dto.Type is "Warehouse" or "DistributionCenter"
            };
        }

        // Pass 2: Link children to parents
        foreach (var dto in flatList)
        {
            var node = nodeMap[dto.Id];

            if (dto.ParentLocationId.HasValue && nodeMap.TryGetValue(dto.ParentLocationId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }
}
