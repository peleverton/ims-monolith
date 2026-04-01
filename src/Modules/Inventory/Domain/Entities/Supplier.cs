using IMS.Modular.Shared.Domain;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Inventory.Domain.Events;

namespace IMS.Modular.Modules.Inventory.Domain.Entities;

/// <summary>
/// Supplier — manages vendor/supplier relationship data.
/// </summary>
public class Supplier : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? ContactPerson { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }
    public string? TaxId { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermsDays { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    private Supplier() { }

    public Supplier(
        string name,
        string code,
        string? contactPerson = null,
        string? email = null,
        string? phone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name;
        Code = code;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;

        AddDomainEvent(new SupplierCreatedEvent(Id, name, code));
    }

    public void Update(
        string name,
        string? contactPerson,
        string? email,
        string? phone,
        string? address,
        string? city,
        string? state,
        string? country,
        string? postalCode,
        string? taxId,
        decimal creditLimit,
        int paymentTermsDays,
        string? notes)
    {
        Name = name;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Address = address;
        City = city;
        State = state;
        Country = country;
        PostalCode = postalCode;
        TaxId = taxId;
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContact(string? contactPerson, string? email, string? phone)
    {
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
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
        AddDomainEvent(new SupplierDeactivatedEvent(Id, Name));
    }
}
