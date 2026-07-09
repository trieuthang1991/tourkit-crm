using FluentValidation;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Application.Commission.Validators;

public sealed class CreateCommissionRuleValidator : AbstractValidator<CreateCommissionRuleDto>
{
    public CreateCommissionRuleValidator() => RuleFor(x => x.Percentage).GreaterThanOrEqualTo(0);
}

public sealed class UpdateCommissionRuleValidator : AbstractValidator<UpdateCommissionRuleDto>
{
    public UpdateCommissionRuleValidator() => RuleFor(x => x.Percentage).GreaterThanOrEqualTo(0);
}
