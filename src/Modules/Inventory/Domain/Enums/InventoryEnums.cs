namespace IMS.Modular.Modules.Inventory.Domain.Enums;

/// <summary>
/// Product categories for inventory classification.
/// </summary>
public enum ProductCategory
{
    Electronics,
    Food,
    Beverages,
    Clothing,
    Furniture,
    Books,
    Toys,
    Sports,
    Tools,
    Automotive,
    Health,
    Medical,
    Beauty,
    Home,
    Garden,
    Office,
    Pet,
    Baby,
    Other
}

/// <summary>
/// Stock status — auto-calculated based on current stock vs min/max levels.
/// </summary>
public enum StockStatus
{
    InStock,
    LowStock,
    OutOfStock,
    Overstock,
    Discontinued
}

/// <summary>
/// Types of stock movements for audit trail.
/// </summary>
public enum StockMovementType
{
    InitialStock,
    StockIn,
    StockOut,
    Adjustment,
    Transfer,
    Sale,
    Purchase,
    Return,
    Damage,
    Loss,
    Expired,
    LocationChanged,
    PriceAdjustment,
    Updated,
    Discontinued
}

/// <summary>
/// Location types for warehouse/storage hierarchy.
/// </summary>
public enum LocationType
{
    Warehouse,
    Store,
    Aisle,
    Shelf,
    DistributionCenter,
    Manufacturing,
    ReturnCenter,
    Transit
}
