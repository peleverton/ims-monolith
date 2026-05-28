using FluentValidation;

namespace IMS.Modular.Shared.MultiTenancy.TenantManagement;

/// <summary>
/// US-092: Body for the self-service tenant signup endpoint (POST /api/tenants/signup).
/// </summary>
public record SignupRequest(
    string OrgName,
    string Email,
    string Password,
    string Plan);

/// <summary>
/// US-092: FluentValidation validator for <see cref="SignupRequest"/>.
/// </summary>
public sealed class SignupValidator : AbstractValidator<SignupRequest>
{
    private static readonly string[] ValidPlans = ["free", "starter", "pro"];

    public SignupValidator()
    {
        RuleFor(x => x.OrgName)
            .NotEmpty().WithMessage("Org name is required.")
            .MinimumLength(3).WithMessage("Org name must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Org name cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Plan is required.")
            .Must(p => ValidPlans.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Plan must be one of: free, starter, pro.");
    }
}
