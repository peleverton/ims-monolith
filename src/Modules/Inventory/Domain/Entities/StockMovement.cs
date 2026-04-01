using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Inventory.Domain.Enums;

namespace IMS.Modular.Modules.Inventory.Domain.Entities;

/// <summary>
/// StockMovement — records every stock change for audit trail.
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime MovementDate { get; private set; } = DateTime.UtcNow;

    private StockMovement() { }

    public StockMovement(
        Guid productId,
        StockMovementType movementType,
        int quantity,
        Guid? locationId = null,
        string? reference = null,
        string? notes = null)
    {
        ProductId = productId;
        MovementType = movementType;
        Quantity = quantity;
        LocationId = locationId;
        Reference = reference;
        Notes = notes;
    }
}
