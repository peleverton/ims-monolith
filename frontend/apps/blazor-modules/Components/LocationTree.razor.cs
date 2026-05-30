using BlazorModules.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BlazorModules.Components;

public partial class LocationTree : ComponentBase
{
    private List<LocationTreeNode> _roots = [];
    private List<TreeItemData<LocationTreeNode>> _treeItems = [];
    private bool _loading = true;
    private string? _error;
    private string _searchTerm = string.Empty;
    private int _totalLocations;
    private int _maxDepth;

    /// <summary>Optional: API base URL passed from the host (Next.js).</summary>
    [Parameter] public string? ApiBaseUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var locations = await LocationService.GetAllLocationsAsync();
            _totalLocations = locations.Count;

            // Flat → Tree transformation (O(N) algorithm)
            _roots = LocationTreeNode.BuildTree(locations);
            _maxDepth = CalculateMaxDepth(_roots, 0);

            // Convert to MudBlazor TreeItemData
            _treeItems = BuildTreeItems(_roots);
        }
        catch (Exception ex)
        {
            _error = $"Erro ao carregar locais: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Recursively converts LocationTreeNode hierarchy into MudBlazor TreeItemData.
    /// </summary>
    private static List<TreeItemData<LocationTreeNode>> BuildTreeItems(List<LocationTreeNode> nodes)
    {
        var items = new List<TreeItemData<LocationTreeNode>>();
        foreach (var node in nodes)
        {
            var children = node.Children.Count > 0 ? BuildTreeItems(node.Children) : null;
            items.Add(new TreeItemData<LocationTreeNode>
            {
                Value = node,
                Children = children,
                Expanded = node.IsExpanded
            });
        }
        return items;
    }

    private static int CalculateMaxDepth(List<LocationTreeNode> nodes, int current)
    {
        if (nodes.Count == 0) return current;
        return nodes.Max(n => CalculateMaxDepth(n.Children, current + 1));
    }

    private static Color GetTypeColor(string type) => type switch
    {
        "Warehouse" => Color.Primary,
        "Aisle" => Color.Secondary,
        "Shelf" => Color.Tertiary,
        "Store" => Color.Info,
        "DistributionCenter" => Color.Warning,
        _ => Color.Default
    };
}
