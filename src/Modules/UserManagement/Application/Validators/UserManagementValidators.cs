using FluentValidation;
using IMS.Modular.Modules.UserManagement.Application.Commands;

namespace IMS.Modular.Modules.UserManagement.Application.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).MaximumLength(100).When(x => x.Department is not null);
        RuleFor(x => x.JobTitle).MaximumLength(100).When(x => x.JobTitle is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.Bio).MaximumLength(500).When(x => x.Bio is not null);
        RuleFor(x => x.TimeZone).MaximumLength(100).When(x => x.TimeZone is not null);
    }
}

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).MaximumLength(100).When(x => x.Department is not null);
        RuleFor(x => x.JobTitle).MaximumLength(100).When(x => x.JobTitle is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.Bio).MaximumLength(500).When(x => x.Bio is not null);
        RuleFor(x => x.TimeZone).MaximumLength(100).When(x => x.TimeZone is not null);
    }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
