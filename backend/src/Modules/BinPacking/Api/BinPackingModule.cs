using IMS.Modular.Modules.BinPacking.Application.DTOs;
using IMS.Modular.Modules.BinPacking.Application.Services;
using IMS.Modular.Modules.BinPacking.Domain;
using IMS.Modular.Modules.BinPacking.Domain.Entities;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.BinPacking.Api;

/// <summary>
/// Epic 2: Smart Bin Packing — Otimização de empacotamento.
/// </summary>
public static class BinPackingModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        MapPackagingTypes(endpoints);
        MapPacking(endpoints);
        return endpoints;
    }

    private static void MapPackagingTypes(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/bin-packing/packaging-types")
            .WithTags("Bin Packing - Packaging Types")
            .RequireAuthorization(Policies.CanManageInventory);

        group.MapGet("/", async (IPackagingTypeRepository repo, CancellationToken ct) =>
        {
            var types = await repo.GetAllAsync(ct);
            return Results.Ok(types.Select(ToDto).ToList());
        }).WithName("GetPackagingTypes");

        group.MapGet("/{id:guid}", async (Guid id, IPackagingTypeRepository repo, CancellationToken ct) =>
        {
            var type = await repo.GetByIdAsync(id, ct);
            return type is null ? Results.NotFound() : Results.Ok(ToDto(type));
        }).WithName("GetPackagingTypeById");

        group.MapPost("/", async (
            [FromBody] CreatePackagingTypeRequest request,
            IPackagingTypeRepository repo,
            CancellationToken ct) =>
        {
            var type = new PackagingType(
                request.Name, request.Code,
                request.MaxLengthCm, request.MaxWidthCm, request.MaxHeightCm,
                request.MaxWeightKg, request.CostPerUnit);

            await repo.AddAsync(type, ct);
            await repo.SaveChangesAsync(ct);
            return Results.Created($"/api/bin-packing/packaging-types/{type.Id}", ToDto(type));
        }).WithName("CreatePackagingType");

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdatePackagingTypeRequest request,
            IPackagingTypeRepository repo,
            CancellationToken ct) =>
        {
            var type = await repo.GetByIdAsync(id, ct);
            if (type is null) return Results.NotFound();

            type.Update(request.Name, request.MaxLengthCm, request.MaxWidthCm,
                request.MaxHeightCm, request.MaxWeightKg, request.CostPerUnit);
            await repo.SaveChangesAsync(ct);
            return Results.Ok(ToDto(type));
        }).WithName("UpdatePackagingType");

        group.MapPatch("/{id:guid}/deactivate", async (Guid id, IPackagingTypeRepository repo, CancellationToken ct) =>
        {
            var type = await repo.GetByIdAsync(id, ct);
            if (type is null) return Results.NotFound();
            type.Deactivate();
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("DeactivatePackagingType");
    }

    private static void MapPacking(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/bin-packing")
            .WithTags("Bin Packing - Suggestions")
            .RequireAuthorization(Policies.CanViewInventory);

        group.MapPost("/suggest", async (
            [FromBody] PackingRequest request,
            IPackagingTypeRepository packagingRepo,
            InventoryDbContext inventoryDb,
            FirstFitDecreasingPacker packer,
            CancellationToken ct) =>
        {
            if (request.Items.Count == 0)
                return Results.BadRequest(new { error = "Nenhum item fornecido." });

            // Load products with dimensions
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var products = await inventoryDb.Products
                .Where(p => productIds.Contains(p.Id) && p.IsActive)
                .ToListAsync(ct);

            // Build packable items
            var packableItems = new List<IPackableItem>();
            var missingDimensions = new List<string>();

            foreach (var reqItem in request.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == reqItem.ProductId);
                if (product is null) continue;

                if (!product.WeightKg.HasValue || !product.LengthCm.HasValue ||
                    !product.WidthCm.HasValue || !product.HeightCm.HasValue)
                {
                    missingDimensions.Add(product.SKU);
                    continue;
                }

                packableItems.Add(new PackableProduct(
                    product.Id, product.Name, product.SKU, reqItem.Quantity,
                    product.WeightKg.Value, product.LengthCm.Value,
                    product.WidthCm.Value, product.HeightCm.Value));
            }

            if (missingDimensions.Count > 0)
            {
                return Results.UnprocessableEntity(new
                {
                    error = "Produtos sem dimensões configuradas.",
                    skus = missingDimensions
                });
            }

            // Get available box types
            var boxes = await packagingRepo.GetActiveOrderedByVolumeAsync(ct);
            if (boxes.Count == 0)
                return Results.UnprocessableEntity(new { error = "Nenhum tipo de embalagem cadastrado." });

            // Run FFD packing algorithm
            var suggestion = packer.Pack(packableItems, boxes);

            return Results.Ok(ToSuggestionDto(suggestion));
        }).WithName("SuggestPacking");
    }

    private static PackagingTypeDto ToDto(PackagingType t) => new(
        t.Id, t.Name, t.Code,
        t.MaxLengthCm, t.MaxWidthCm, t.MaxHeightCm, t.MaxWeightKg,
        t.MaxLengthCm * t.MaxWidthCm * t.MaxHeightCm,
        t.CostPerUnit, t.IsActive);

    private static PackingSuggestionDto ToSuggestionDto(PackingSuggestion s) => new(
        s.TotalBoxes,
        s.TotalCost,
        s.Boxes.Select(b => new PackedBoxDto(
            b.BoxType.Name, b.BoxType.Code, b.BoxType.CostPerUnit,
            b.UsedVolumeCm3, b.BoxType.CapacityVolumeCm3,
            b.VolumeUtilizationPercent,
            b.UsedWeightKg, b.BoxType.MaxWeightKg,
            b.Items.Select(i => new PackedItemDto(
                i.ItemId, i.SKU, i.ItemName, i.Quantity,
                i.TotalWeightKg, i.TotalVolumeCm3)).ToList()
        )).ToList(),
        s.UnpackableItems.Select(i => new PackedItemDto(
            i.ItemId, i.SKU, i.ItemName, i.Quantity,
            i.TotalWeightKg, i.TotalVolumeCm3)).ToList());
}
