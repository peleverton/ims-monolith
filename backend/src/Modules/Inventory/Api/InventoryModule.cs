using FluentValidation;
using IMS.Modular.Modules.Inventory.Application.Commands;
using IMS.Modular.Modules.Inventory.Application.DTOs;
using IMS.Modular.Modules.Inventory.Application.Queries;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.Inventory.Api;

public class InventoryModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        MapProducts(endpoints);
        MapStockMovements(endpoints);
        MapSuppliers(endpoints);
        MapLocations(endpoints);
        return endpoints;
    }

    // ── Products ─────────────────────────────────────────────────────────

    private static void MapProducts(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/inventory/products")
            .WithTags("Inventory - Products")
            .RequireAuthorization();

        group.MapGet("/", GetProducts).WithName("GetProducts");
        group.MapGet("/{id:guid}", GetProductById).WithName("GetProductById");
        group.MapGet("/sku/{sku}", GetProductBySku).WithName("GetProductBySku");
        group.MapPost("/", CreateProduct).WithName("CreateProduct");
        group.MapPut("/{id:guid}", UpdateProduct).WithName("UpdateProduct");
        group.MapPatch("/{id:guid}/pricing", UpdatePricing).WithName("UpdateProductPricing");
        group.MapPatch("/{id:guid}/stock/adjust", AdjustStock).WithName("AdjustStock");
        group.MapPatch("/{id:guid}/stock/transfer", TransferStock).WithName("TransferStock");
        group.MapPatch("/{id:guid}/discontinue", DiscontinueProduct).WithName("DiscontinueProduct");
        group.MapDelete("/{id:guid}", DeleteProduct).WithName("DeleteProduct");
    }

    private static async Task<IResult> GetProducts(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        ProductCategory? categoryEnum = null;
        if (!string.IsNullOrEmpty(category) && Enum.TryParse<ProductCategory>(category, true, out var pc))
            categoryEnum = pc;

        StockStatus? stockStatusEnum = null;
        if (!string.IsNullOrEmpty(stockStatus) && Enum.TryParse<StockStatus>(stockStatus, true, out var ss))
            stockStatusEnum = ss;

        var query = new GetProductsQuery(page, pageSize, categoryEnum, stockStatusEnum, locationId, supplierId, search);
        return Results.Ok(await mediator.Send(query, ct));
    }

    private static async Task<IResult> GetProductById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetProductBySku(string sku, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductBySkuQuery(sku), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        IValidator<CreateProductRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new CreateProductCommand(
            request.Name, request.SKU, request.Category,
            request.MinimumStockLevel, request.MaximumStockLevel,
            request.UnitPrice, request.CostPrice,
            request.Description, request.Barcode,
            request.Unit, request.Currency,
            request.LocationId, request.SupplierId, request.ExpiryDate);

        var result = await mediator.Send(command, ct);
        return result.ToCreatedResult($"/api/inventory/products/{result.Value?.Id}", httpContext);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        IValidator<UpdateProductRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new UpdateProductCommand(
            id, request.Name, request.Description, request.Category,
            request.MinimumStockLevel, request.MaximumStockLevel,
            request.Barcode, request.Unit, request.Currency,
            request.LocationId, request.SupplierId, request.ExpiryDate);

        var result = await mediator.Send(command, ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> UpdatePricing(
        Guid id,
        UpdatePricingRequest request,
        IValidator<UpdatePricingRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var result = await mediator.Send(new UpdatePricingCommand(id, request.UnitPrice, request.CostPrice), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> AdjustStock(
        Guid id,
        AdjustStockRequest request,
        IValidator<AdjustStockRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var result = await mediator.Send(
            new AdjustStockCommand(id, request.Quantity, request.MovementType, request.Reference, request.Notes), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> TransferStock(
        Guid id,
        TransferStockRequest request,
        IValidator<TransferStockRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var result = await mediator.Send(
            new TransferStockCommand(id, request.Quantity, request.ToLocationId, request.Notes), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DiscontinueProduct(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DiscontinueProductCommand(id), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeleteProduct(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(id), ct);
        if (!result.IsSuccess) return result.ToApiResult(httpContext);
        return Results.NoContent();
    }

    // ── Stock Movements ───────────────────────────────────────────────────

    private static void MapStockMovements(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/inventory/stock-movements")
            .WithTags("Inventory - Stock Movements")
            .RequireAuthorization();

        group.MapGet("/", GetStockMovements).WithName("GetStockMovements");
        group.MapPost("/", CreateStockMovement).WithName("CreateStockMovement");
        group.MapPost("/bulk", BulkCreateStockMovements).WithName("BulkCreateStockMovements");
        group.MapDelete("/{id:guid}", DeleteStockMovement).WithName("DeleteStockMovement");
    }

    private static async Task<IResult> GetStockMovements(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        StockMovementType? movementTypeEnum = null;
        if (!string.IsNullOrEmpty(movementType) && Enum.TryParse<StockMovementType>(movementType, true, out var mt))
            movementTypeEnum = mt;

        var query = new GetStockMovementsQuery(page, pageSize, productId, movementTypeEnum, fromDate, toDate);
        return Results.Ok(await mediator.Send(query, ct));
    }

    private static async Task<IResult> CreateStockMovement(
        CreateStockMovementRequest request,
        IValidator<CreateStockMovementRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new CreateStockMovementCommand(
            request.ProductId, request.MovementType, request.Quantity,
            request.LocationId, request.Reference, request.Notes);

        var result = await mediator.Send(command, ct);
        return result.ToCreatedResult($"/api/inventory/stock-movements/{result.Value?.Id}", httpContext);
    }

    private static async Task<IResult> BulkCreateStockMovements(
        BulkCreateStockMovementRequest request,
        IValidator<BulkCreateStockMovementRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var items = request.Movements
            .Select(m => new CreateStockMovementCommand(m.ProductId, m.MovementType, m.Quantity, m.LocationId, m.Reference, m.Notes))
            .ToList();

        var command = new BulkCreateStockMovementCommand(items);
        var result = await mediator.Send(command, ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeleteStockMovement(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteStockMovementCommand(id), ct);
        if (!result.IsSuccess) return result.ToApiResult(httpContext);
        return Results.NoContent();
    }

    // ── Suppliers ─────────────────────────────────────────────────────────

    private static void MapSuppliers(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/inventory/suppliers")
            .WithTags("Inventory - Suppliers")
            .RequireAuthorization();

        group.MapGet("/", GetSuppliers).WithName("GetSuppliers");
        group.MapGet("/{id:guid}", GetSupplierById).WithName("GetSupplierById");
        group.MapPost("/", CreateSupplier).WithName("CreateSupplier");
        group.MapPut("/{id:guid}", UpdateSupplier).WithName("UpdateSupplier");
        group.MapPatch("/{id:guid}/activate", ActivateSupplier).WithName("ActivateSupplier");
        group.MapPatch("/{id:guid}/deactivate", DeactivateSupplier).WithName("DeactivateSupplier");
        group.MapDelete("/{id:guid}", DeleteSupplier).WithName("DeleteSupplier");
    }

    private static async Task<IResult> GetSuppliers(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        return Results.Ok(await mediator.Send(new GetSuppliersQuery(page, pageSize, isActive, search), ct));
    }

    private static async Task<IResult> GetSupplierById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSupplierByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateSupplier(
        CreateSupplierRequest request,
        IValidator<CreateSupplierRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new CreateSupplierCommand(
            request.Name, request.Code, request.ContactPerson, request.Email, request.Phone,
            request.Address, request.City, request.State, request.Country, request.PostalCode,
            request.TaxId, request.CreditLimit, request.PaymentTermsDays, request.Notes);

        var result = await mediator.Send(command, ct);
        return result.ToCreatedResult($"/api/inventory/suppliers/{result.Value?.Id}", httpContext);
    }

    private static async Task<IResult> UpdateSupplier(
        Guid id,
        UpdateSupplierRequest request,
        IValidator<UpdateSupplierRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new UpdateSupplierCommand(
            id, request.Name, request.ContactPerson, request.Email, request.Phone,
            request.Address, request.City, request.State, request.Country, request.PostalCode,
            request.TaxId, request.CreditLimit, request.PaymentTermsDays, request.Notes);

        var result = await mediator.Send(command, ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> ActivateSupplier(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new ActivateSupplierCommand(id), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeactivateSupplier(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivateSupplierCommand(id), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeleteSupplier(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteSupplierCommand(id), ct);
        if (!result.IsSuccess) return result.ToApiResult(httpContext);
        return Results.NoContent();
    }

    // ── Locations ─────────────────────────────────────────────────────────

    private static void MapLocations(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/inventory/locations")
            .WithTags("Inventory - Locations")
            .RequireAuthorization();

        group.MapGet("/", GetLocations).WithName("GetLocations");
        group.MapGet("/{id:guid}", GetLocationById).WithName("GetLocationById");
        group.MapPost("/", CreateLocation).WithName("CreateLocation");
        group.MapPut("/{id:guid}", UpdateLocation).WithName("UpdateLocation");
        group.MapPatch("/{id:guid}/capacity", UpdateLocationCapacity).WithName("UpdateLocationCapacity");
        group.MapPatch("/{id:guid}/activate", ActivateLocation).WithName("ActivateLocation");
        group.MapPatch("/{id:guid}/deactivate", DeactivateLocation).WithName("DeactivateLocation");
        group.MapDelete("/{id:guid}", DeleteLocation).WithName("DeleteLocation");
    }

    private static async Task<IResult> GetLocations(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        LocationType? typeEnum = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<LocationType>(type, true, out var lt))
            typeEnum = lt;

        return Results.Ok(await mediator.Send(new GetLocationsQuery(page, pageSize, typeEnum, isActive, search), ct));
    }

    private static async Task<IResult> GetLocationById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLocationByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateLocation(
        CreateLocationRequest request,
        IValidator<CreateLocationRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new CreateLocationCommand(
            request.Name, request.Code, request.Type, request.Capacity,
            request.Description, request.ParentLocationId,
            request.Address, request.City, request.State, request.Country, request.PostalCode);

        var result = await mediator.Send(command, ct);
        return result.ToCreatedResult($"/api/inventory/locations/{result.Value?.Id}", httpContext);
    }

    private static async Task<IResult> UpdateLocation(
        Guid id,
        UpdateLocationRequest request,
        IValidator<UpdateLocationRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var command = new UpdateLocationCommand(
            id, request.Name, request.Description, request.Type, request.Capacity,
            request.ParentLocationId, request.Address, request.City,
            request.State, request.Country, request.PostalCode);

        var result = await mediator.Send(command, ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> UpdateLocationCapacity(
        Guid id,
        UpdateCapacityRequest request,
        IValidator<UpdateCapacityRequest> validator,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return validation.ToDictionary().ToValidationProblem(httpContext);

        var result = await mediator.Send(new UpdateLocationCapacityCommand(id, request.Capacity), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> ActivateLocation(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new ActivateLocationCommand(id), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeactivateLocation(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivateLocationCommand(id), ct);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeleteLocation(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteLocationCommand(id), ct);
        if (!result.IsSuccess) return result.ToApiResult(httpContext);
        return Results.NoContent();
    }
}
