using IMS.Modular.Modules.BinPacking.Domain;
using IMS.Modular.Modules.BinPacking.Domain.Entities;

namespace IMS.Modular.Modules.BinPacking.Application.Services;

/// <summary>
/// Algoritmo First Fit Decreasing (FFD) para bin packing.
/// 
/// Complexidade: O(n log n) para sort + O(n*m) para alocação.
/// Heurística prática que garante resultado ≤ 11/9 OPT + 6/9 no pior caso.
///
/// Approach:
/// 1. Ordenar itens por volume decrescente (itens maiores primeiro)
/// 2. Para cada item, tentar encaixar na primeira caixa aberta que comporta
/// 3. Se nenhuma caixa aberta comporta, abrir nova caixa (menor disponível que caiba)
/// 4. Se nenhum tipo de caixa comporta o item, marcar como "unpackable"
/// </summary>
public sealed class FirstFitDecreasingPacker
{
    /// <summary>
    /// Sugere o empacotamento ótimo usando FFD.
    /// </summary>
    /// <param name="items">Itens a empacotar</param>
    /// <param name="availableBoxes">Tipos de caixa disponíveis, ordenados por volume crescente</param>
    public PackingSuggestion Pack(IReadOnlyList<IPackableItem> items, IReadOnlyList<PackagingType> availableBoxes)
    {
        var builder = new PackingSuggestionBuilder();
        var openBoxes = new List<PackedBox>();

        // Sort by volume descending (FFD heuristic)
        var sorted = items
            .OrderByDescending(i => i.TotalVolumeCm3)
            .ToList();

        foreach (var item in sorted)
        {
            // Try to find the smallest available box type that can hold this item alone
            var fitsAnyBox = availableBoxes.Any(b =>
                item.TotalVolumeCm3 <= b.CapacityVolumeCm3 && item.TotalWeightKg <= b.MaxWeightKg);

            if (!fitsAnyBox)
            {
                builder.MarkUnpackable(item);
                continue;
            }

            // Try to fit in an already-open box (first fit)
            var placed = false;
            foreach (var box in openBoxes)
            {
                if (box.CanFit(item))
                {
                    box.Items.Add(item);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // Open a new box — pick smallest box type that fits this item
                var bestBox = availableBoxes.FirstOrDefault(b =>
                    item.TotalVolumeCm3 <= b.CapacityVolumeCm3 && item.TotalWeightKg <= b.MaxWeightKg);

                if (bestBox is not null)
                {
                    var newBox = new PackedBox { BoxType = bestBox };
                    newBox.Items.Add(item);
                    openBoxes.Add(newBox);
                }
            }
        }

        // Build result from open boxes
        foreach (var box in openBoxes)
        {
            builder.AddBox(box.BoxType);
            foreach (var item in box.Items)
                builder.AddItemToLastBox(item);
        }

        return builder.Build();
    }
}
