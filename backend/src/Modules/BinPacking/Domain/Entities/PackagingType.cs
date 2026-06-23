using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Modules.BinPacking.Domain.Entities;

/// <summary>
/// Tipo de embalagem disponível para empacotamento.
/// Ex: "Caixa Pequena", "Caixa Média", "Envelope Reforçado".
/// </summary>
public class PackagingType : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;

    // ── Dimensões internas da caixa ─────────────────────────────────
    public decimal MaxLengthCm { get; private set; }
    public decimal MaxWidthCm { get; private set; }
    public decimal MaxHeightCm { get; private set; }
    public decimal MaxWeightKg { get; private set; }

    /// <summary>Volume interno em cm³.</summary>
    public decimal CapacityVolumeCm3 => MaxLengthCm * MaxWidthCm * MaxHeightCm;

    /// <summary>Custo unitário da embalagem.</summary>
    public decimal CostPerUnit { get; private set; }

    public bool IsActive { get; private set; } = true;

    private PackagingType() { }

    public PackagingType(
        string name,
        string code,
        decimal maxLengthCm,
        decimal maxWidthCm,
        decimal maxHeightCm,
        decimal maxWeightKg,
        decimal costPerUnit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name;
        Code = code;
        MaxLengthCm = maxLengthCm;
        MaxWidthCm = maxWidthCm;
        MaxHeightCm = maxHeightCm;
        MaxWeightKg = maxWeightKg;
        CostPerUnit = costPerUnit;
    }

    public void Update(string name, decimal maxLengthCm, decimal maxWidthCm, decimal maxHeightCm, decimal maxWeightKg, decimal costPerUnit)
    {
        Name = name;
        MaxLengthCm = maxLengthCm;
        MaxWidthCm = maxWidthCm;
        MaxHeightCm = maxHeightCm;
        MaxWeightKg = maxWeightKg;
        CostPerUnit = costPerUnit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}
