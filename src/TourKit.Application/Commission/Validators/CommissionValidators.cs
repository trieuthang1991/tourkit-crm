using FluentValidation;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Application.Commission.Validators;

public sealed class CreateProfitShareValidator : AbstractValidator<CreateProfitShareDto>
{
    public CreateProfitShareValidator()
    {
        RuleFor(x => x.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
