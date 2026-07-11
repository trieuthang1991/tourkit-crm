using FluentValidation;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Application.Commission.Validators;

public sealed class CreateCustomerCommissionRuleValidator : AbstractValidator<CreateCustomerCommissionRuleDto>
{
    public CreateCustomerCommissionRuleValidator()
    {
        RuleFor(x => x.Percentage).InclusiveBetween(0m, 100m);
    }
}

public sealed class UpdateCustomerCommissionRuleValidator : AbstractValidator<UpdateCustomerCommissionRuleDto>
{
    public UpdateCustomerCommissionRuleValidator()
    {
        RuleFor(x => x.Percentage).InclusiveBetween(0m, 100m);
    }
}
