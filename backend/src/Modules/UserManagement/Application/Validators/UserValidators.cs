using FluentValidation;
using IMS.Modular.Modules.UserManagement.Application.DTOs;

namespace IMS.Modular.Modules.UserManagement.Application.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
    }
}

public class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequest>
{
    private static readonly string[] ValidRoles = ["Admin", "Manager", "User"];

    public ChangeUserRoleRequestValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .Must(r => ValidRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", ValidRoles)}");
    }
}

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("Username may only contain letters, numbers, _, . and -");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.TemporaryPassword)
            .MinimumLength(8).When(x => x.TemporaryPassword is not null)
            .WithMessage("Temporary password must be at least 8 characters");
    }
}
