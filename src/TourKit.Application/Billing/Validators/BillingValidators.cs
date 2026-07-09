using FluentValidation;
using TourKit.Application.Billing.Dtos;

namespace TourKit.Application.Billing.Validators;

public sealed class ChangePlanValidator : AbstractValidator<ChangePlanDto>
{
    public ChangePlanValidator() => RuleFor(x => x.PlanCode).NotEmpty();
}
