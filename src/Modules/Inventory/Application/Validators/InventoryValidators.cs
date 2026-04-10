using FluentValidation;
using IMS.Modular.Modules.Inventory.Application.DTOs;

namespace IMS.Modular.Modules.Inventory.Application.Validators;

// ── Product Validators ────────────────────────────────────────────────────

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid product category");

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level must be zero or greater");

        RuleFor(x => x.MaximumStockLevel)
            .GreaterThan(x => x.MinimumStockLevel)
            .WithMessage("Maximum stock level must be greater than minimum stock level");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be zero or greater");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be zero or greater");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(20).WithMessage("Unit must not exceed 20 characters");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode must not exceed 100 characters")
            .When(x => x.Barcode is not null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.ExpiryDate)
            .Must(d => !d.HasValue || d.Value > DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future")
            .When(x => x.ExpiryDate.HasValue);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid product category");

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level must be zero or greater");

        RuleFor(x => x.MaximumStockLevel)
            .GreaterThan(x => x.MinimumStockLevel)
            .WithMessage("Maximum stock level must be greater than minimum stock level");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required")
            .MaximumLength(20).WithMessage("Unit must not exceed 20 characters");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode must not exceed 100 characters")
            .When(x => x.Barcode is not null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.ExpiryDate)
            .Must(d => !d.HasValue || d.Value > DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future")
            .When(x => x.ExpiryDate.HasValue);
    }
}

public sealed class UpdatePricingRequestValidator : AbstractValidator<UpdatePricingRequest>
{
    public UpdatePricingRequestValidator()
    {
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be zero or greater");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be zero or greater");
    }
}

public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.MovementType)
            .IsInEnum().WithMessage("Invalid movement type");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("Reference must not exceed 100 characters")
            .When(x => x.Reference is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => x.Notes is not null);
    }
}

public sealed class TransferStockRequestValidator : AbstractValidator<TransferStockRequest>
{
    public TransferStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.ToLocationId)
            .NotEmpty().WithMessage("Destination location is required");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => x.Notes is not null);
    }
}

// ── Stock Movement Validators ─────────────────────────────────────────────

public sealed class CreateStockMovementRequestValidator : AbstractValidator<CreateStockMovementRequest>
{
    public CreateStockMovementRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product is required");

        RuleFor(x => x.MovementType)
            .IsInEnum().WithMessage("Invalid movement type");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("Reference must not exceed 100 characters")
            .When(x => x.Reference is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => x.Notes is not null);
    }
}

public sealed class BulkCreateStockMovementRequestValidator : AbstractValidator<BulkCreateStockMovementRequest>
{
    public BulkCreateStockMovementRequestValidator()
    {
        RuleFor(x => x.Movements)
            .NotEmpty().WithMessage("At least one stock movement is required")
            .Must(m => m.Count <= 100).WithMessage("Cannot process more than 100 movements at once");

        RuleForEach(x => x.Movements).SetValidator(new CreateStockMovementRequestValidator());
    }
}

// ── Supplier Validators ───────────────────────────────────────────────────

public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone must not exceed 30 characters")
            .When(x => x.Phone is not null);

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit must be zero or greater");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).WithMessage("Payment terms must be zero or greater")
            .LessThanOrEqualTo(365).WithMessage("Payment terms must not exceed 365 days");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters")
            .When(x => x.Notes is not null);
    }
}

public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone must not exceed 30 characters")
            .When(x => x.Phone is not null);

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit must be zero or greater");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).WithMessage("Payment terms must be zero or greater")
            .LessThanOrEqualTo(365).WithMessage("Payment terms must not exceed 365 days");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters")
            .When(x => x.Notes is not null);
    }
}

// ── Location Validators ───────────────────────────────────────────────────

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid location type");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than zero");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);
    }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid location type");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than zero");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);
    }
}

public sealed class UpdateCapacityRequestValidator : AbstractValidator<UpdateCapacityRequest>
{
    public UpdateCapacityRequestValidator()
    {
        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than zero");
    }
}
