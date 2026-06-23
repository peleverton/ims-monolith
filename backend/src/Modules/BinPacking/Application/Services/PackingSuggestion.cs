using IMS.Modular.Modules.BinPacking.Domain;
using IMS.Modular.Modules.BinPacking.Domain.Entities;

namespace IMS.Modular.Modules.BinPacking.Application.Services;

/// <summary>
/// Resultado da sugestão de empacotamento.
/// </summary>
public sealed class PackingSuggestion
{
    public List<PackedBox> Boxes { get; } = [];
    public decimal TotalCost => Boxes.Sum(b => b.BoxCost);
    public int TotalBoxes => Boxes.Count;
    public List<IPackableItem> UnpackableItems { get; } = [];
}

public sealed class PackedBox
{
    public PackagingType BoxType { get; init; } = null!;
    public List<IPackableItem> Items { get; } = [];
    public decimal UsedVolumeCm3 => Items.Sum(i => i.TotalVolumeCm3);
    public decimal UsedWeightKg => Items.Sum(i => i.TotalWeightKg);
    public decimal RemainingVolumeCm3 => BoxType.CapacityVolumeCm3 - UsedVolumeCm3;
    public decimal RemainingWeightKg => BoxType.MaxWeightKg - UsedWeightKg;
    public decimal BoxCost => BoxType.CostPerUnit;
    public decimal VolumeUtilizationPercent => BoxType.CapacityVolumeCm3 > 0
        ? Math.Round(UsedVolumeCm3 / BoxType.CapacityVolumeCm3 * 100, 1)
        : 0;

    public bool CanFit(IPackableItem item)
        => item.TotalVolumeCm3 <= RemainingVolumeCm3
           && item.TotalWeightKg <= RemainingWeightKg;
}

/// <summary>
/// Builder Pattern: constrói uma PackingSuggestion passo a passo.
/// </summary>
public sealed class PackingSuggestionBuilder
{
    private readonly PackingSuggestion _suggestion = new();

    public PackingSuggestionBuilder AddBox(PackagingType boxType)
    {
        _suggestion.Boxes.Add(new PackedBox { BoxType = boxType });
        return this;
    }

    public PackingSuggestionBuilder AddItemToLastBox(IPackableItem item)
    {
        var lastBox = _suggestion.Boxes.LastOrDefault();
        lastBox?.Items.Add(item);
        return this;
    }

    public PackingSuggestionBuilder MarkUnpackable(IPackableItem item)
    {
        _suggestion.UnpackableItems.Add(item);
        return this;
    }

    public PackingSuggestion Build() => _suggestion;
}
