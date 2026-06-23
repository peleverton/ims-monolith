using IMS.Modular.Modules.BinPacking.Domain.Entities;

namespace IMS.Modular.Modules.BinPacking.Application.DTOs;

public record PackagingTypeDto(
    Guid Id,
    string Name,
    string Code,
    decimal MaxLengthCm,
    decimal MaxWidthCm,
    decimal MaxHeightCm,
    decimal MaxWeightKg,
    decimal CapacityVolumeCm3,
    decimal CostPerUnit,
    bool IsActive);

public record CreatePackagingTypeRequest(
    string Name,
    string Code,
    decimal MaxLengthCm,
    decimal MaxWidthCm,
    decimal MaxHeightCm,
    decimal MaxWeightKg,
    decimal CostPerUnit);

public record UpdatePackagingTypeRequest(
    string Name,
    decimal MaxLengthCm,
    decimal MaxWidthCm,
    decimal MaxHeightCm,
    decimal MaxWeightKg,
    decimal CostPerUnit);

public record PackingRequestItem(
    Guid ProductId,
    int Quantity);

public record PackingRequest(List<PackingRequestItem> Items);

public record PackedBoxDto(
    string BoxName,
    string BoxCode,
    decimal BoxCostPerUnit,
    decimal UsedVolumeCm3,
    decimal CapacityVolumeCm3,
    decimal VolumeUtilizationPercent,
    decimal UsedWeightKg,
    decimal MaxWeightKg,
    List<PackedItemDto> Items);

public record PackedItemDto(
    Guid ProductId,
    string SKU,
    string Name,
    int Quantity,
    decimal WeightKg,
    decimal VolumeCm3);

public record PackingSuggestionDto(
    int TotalBoxes,
    decimal TotalCost,
    List<PackedBoxDto> Boxes,
    List<PackedItemDto> UnpackableItems);
