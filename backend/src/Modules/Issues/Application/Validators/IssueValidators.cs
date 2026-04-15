using FluentValidation;
using IMS.Modular.Modules.Issues.Application.DTOs;

namespace IMS.Modular.Modules.Issues.Application.Validators;

public sealed class CreateIssueRequestValidator : AbstractValidator<CreateIssueRequest>
{
    public CreateIssueRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value");

        RuleFor(x => x.DueDate)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("DueDate must be in the future");
    }
}

public sealed class UpdateIssueRequestValidator : AbstractValidator<UpdateIssueRequest>
{
    public UpdateIssueRequestValidator()
    {
        RuleFor(x => x.Title)
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.DueDate)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("DueDate must be in the future")
            .When(x => x.DueDate.HasValue);
    }
}

public sealed class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    private static readonly string[] ValidStatuses = ["Open", "InProgress", "Testing", "Resolved", "Closed"];

    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}

public sealed class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MinimumLength(1).WithMessage("Comment must not be empty")
            .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters");
    }
}

public sealed class AssignIssueRequestValidator : AbstractValidator<AssignIssueRequest>
{
    public AssignIssueRequestValidator()
    {
        RuleFor(x => x.AssigneeId)
            .NotEmpty().WithMessage("AssigneeId is required");
    }
}

public sealed class AddTagRequestValidator : AbstractValidator<AddTagRequest>
{
    public AddTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required")
            .MaximumLength(50).WithMessage("Tag name must not exceed 50 characters");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Tag color is required")
            .Matches("^#[0-9a-fA-F]{6}$").WithMessage("Color must be a valid hex code (e.g. #FF5733)");
    }
}
