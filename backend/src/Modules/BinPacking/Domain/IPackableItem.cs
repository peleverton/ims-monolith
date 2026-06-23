namespace IMS.Modular.Modules.BinPacking.Domain;

/// <summary>
/// Composite Pattern: interface unificada para itens empacotáveis.
/// Tanto um Product individual quanto um Kit (composto) implementam esta interface.
/// </summary>
public interface IPackableItem
{
    Guid ItemId { get; }
    string ItemName { get; }
    string SKU { get; }
    int Quantity { get; }
    decimal WeightKg { get; }
    decimal LengthCm { get; }
    decimal WidthCm { get; }
    decimal HeightCm { get; }
    decimal VolumeCm3 => LengthCm * WidthCm * HeightCm;
    decimal TotalWeightKg => WeightKg * Quantity;
    decimal TotalVolumeCm3 => VolumeCm3 * Quantity;
}

/// <summary>Product individual como item empacotável (Leaf do Composite).</summary>
public sealed class PackableProduct : IPackableItem
{
    public Guid ItemId { get; }
    public string ItemName { get; }
    public string SKU { get; }
    public int Quantity { get; }
    public decimal WeightKg { get; }
    public decimal LengthCm { get; }
    public decimal WidthCm { get; }
    public decimal HeightCm { get; }

    public PackableProduct(
        Guid itemId, string itemName, string sku, int quantity,
        decimal weightKg, decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        ItemId = itemId;
        ItemName = itemName;
        SKU = sku;
        Quantity = quantity;
        WeightKg = weightKg;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }
}

/// <summary>Kit composto de múltiplos itens (Composite do Composite Pattern).</summary>
public sealed class PackableKit : IPackableItem
{
    public Guid ItemId { get; }
    public string ItemName { get; }
    public string SKU { get; }
    public int Quantity { get; } = 1;

    private readonly List<IPackableItem> _items = [];
    public IReadOnlyList<IPackableItem> Items => _items.AsReadOnly();

    public decimal WeightKg => _items.Sum(i => i.TotalWeightKg);
    public decimal LengthCm => _items.Max(i => i.LengthCm);
    public decimal WidthCm => _items.Max(i => i.WidthCm);
    public decimal HeightCm => _items.Sum(i => i.HeightCm * i.Quantity);

    public PackableKit(Guid id, string name, string sku)
    {
        ItemId = id;
        ItemName = name;
        SKU = sku;
    }

    public void AddItem(IPackableItem item) => _items.Add(item);
}
