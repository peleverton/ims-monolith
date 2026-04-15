using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using Domain = IMS.Modular.Modules.Inventory.Domain;

namespace IMS.Modular.Modules.Inventory.Application.Mappings;

public static class InventoryMapper
{
    // ── From EF Core entities ────────────────────────────────────────

    public static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.SKU, p.Barcode, p.Description,
        p.Category.ToString(), p.CurrentStock, p.MinimumStockLevel, p.MaximumStockLevel,
        p.UnitPrice, p.CostPrice, p.Unit, p.Currency,
        p.LocationId, p.SupplierId, p.ExpiryDate,
        p.StockStatus.ToString(), p.IsActive, p.CreatedAt, p.UpdatedAt);

    public static StockMovementDto ToDto(StockMovement m, string? productName = null, string? productSku = null, string? locationName = null) => new(
        m.Id, m.ProductId, productName, productSku,
        m.MovementType.ToString(), m.Quantity,
        m.LocationId, locationName, m.Reference, m.Notes, m.MovementDate);

    public static SupplierDto ToDto(Supplier s) => new(
        s.Id, s.Name, s.Code, s.ContactPerson, s.Email, s.Phone,
        s.Address, s.City, s.State, s.Country, s.PostalCode,
        s.TaxId, s.CreditLimit, s.PaymentTermsDays,
        s.IsActive, s.Notes, s.CreatedAt, s.UpdatedAt);

    public static LocationDto ToDto(Location l) => new(
        l.Id, l.Name, l.Code, l.Type.ToString(), l.Capacity,
        l.Description, l.ParentLocationId, l.Address, l.City, l.State,
        l.Country, l.PostalCode, l.IsActive, l.CreatedAt, l.UpdatedAt);

    // ── From Dapper read DTOs ────────────────────────────────────────

    public static ProductDto FromReadDto(Domain.ProductReadDto r) => new(
        r.Id, r.Name, r.SKU, r.Barcode, r.Description,
        r.Category, r.CurrentStock, r.MinimumStockLevel, r.MaximumStockLevel,
        r.UnitPrice, r.CostPrice, r.Unit, r.Currency,
        r.LocationId, r.SupplierId, r.ExpiryDate,
        r.StockStatus, r.IsActive, r.CreatedAt, r.UpdatedAt);

    public static ProductListDto FromSummaryDto(Domain.ProductSummaryDto r) => new(
        r.Id, r.Name, r.SKU, r.Category, r.CurrentStock, r.UnitPrice, r.StockStatus, r.IsActive, r.CreatedAt);

    public static StockMovementDto FromReadDto(Domain.StockMovementReadDto r) => new(
        r.Id, r.ProductId, r.ProductName, r.ProductSKU,
        r.MovementType, r.Quantity, r.LocationId, r.LocationName,
        r.Reference, r.Notes, r.MovementDate);

    public static SupplierDto FromReadDto(Domain.SupplierReadDto r) => new(
        r.Id, r.Name, r.Code, r.ContactPerson, r.Email, r.Phone,
        r.Address, r.City, r.State, r.Country, r.PostalCode,
        r.TaxId, r.CreditLimit, r.PaymentTermsDays,
        r.IsActive, r.Notes, r.CreatedAt, r.UpdatedAt);

    public static SupplierListDto FromSummaryDto(Domain.SupplierSummaryDto r) => new(
        r.Id, r.Name, r.Code, r.ContactPerson, r.Email, r.IsActive, r.CreatedAt);

    public static LocationDto FromReadDto(Domain.LocationReadDto r) => new(
        r.Id, r.Name, r.Code, r.Type, r.Capacity, r.Description,
        r.ParentLocationId, r.Address, r.City, r.State,
        r.Country, r.PostalCode, r.IsActive, r.CreatedAt, r.UpdatedAt);

    public static LocationListDto FromSummaryDto(Domain.LocationSummaryDto r) => new(
        r.Id, r.Name, r.Code, r.Type, r.Capacity, r.IsActive, r.CreatedAt);
}
