using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Domain.Events;

namespace IMS.Modular.Modules.Inventory.Domain.Entities;

/// <summary>
/// Location — hierarchical warehouse/storage location entity.
/// Supports parent-child relationships and capacity tracking.
/// </summary>
public class Location : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public LocationType Type { get; private set; }
    public int Capacity { get; private set; }
    public string? Description { get; private set; }
    public Guid? ParentLocationId { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Location() { }

    public Location(
        string name,
        string code,
        LocationType type,
        int capacity,
        string? description = null,
        Guid? parentLocationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name;
        Code = code;
        Type = type;
        Capacity = capacity;
        Description = description;
        ParentLocationId = parentLocationId;

        AddDomainEvent(new LocationCreatedEvent(Id, name, code, type));
    }

    public void Update(
        string name,
        string? description,
        LocationType type,
        int capacity,
        Guid? parentLocationId,
        string? address,
        string? city,
        string? state,
        string? country,
        string? postalCode)
    {
        Name = name;
        Description = description;
        Type = type;
        Capacity = capacity;
        ParentLocationId = parentLocationId;
        Address = address;
        City = city;
        State = state;
        Country = country;
        PostalCode = postalCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCapacity(int capacity)
    {
        Capacity = capacity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new LocationDeactivatedEvent(Id, Name));
    }
}
