using FluentValidation;
using IMS.Modular.Modules.InventoryIssues.Application.Commands;

namespace IMS.Modular.Modules.InventoryIssues.Application.Validators;

public class CreateInventoryIssueValidator : AbstractValidator<CreateInventoryIssueCommand>
{
    public CreateInventoryIssueValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ReporterId).NotEmpty();
        RuleFor(x => x.AffectedQuantity).GreaterThan(0).When(x => x.AffectedQuantity.HasValue);
        RuleFor(x => x.EstimatedLoss).GreaterThanOrEqualTo(0).When(x => x.EstimatedLoss.HasValue);
        RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow).When(x => x.DueDate.HasValue);
    }
}

public class UpdateInventoryIssueValidator : AbstractValidator<UpdateInventoryIssueCommand>
{
    public UpdateInventoryIssueValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.AffectedQuantity).GreaterThan(0).When(x => x.AffectedQuantity.HasValue);
        RuleFor(x => x.EstimatedLoss).GreaterThanOrEqualTo(0).When(x => x.EstimatedLoss.HasValue);
    }
}

public class AssignInventoryIssueValidator : AbstractValidator<AssignInventoryIssueCommand>
{
    public AssignInventoryIssueValidator()
    {
        RuleFor(x => x.AssigneeId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
