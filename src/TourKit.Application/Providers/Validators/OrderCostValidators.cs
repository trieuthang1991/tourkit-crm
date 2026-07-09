using FluentValidation;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers.Validators;

public sealed class CreateOrderCostValidator : AbstractValidator<CreateOrderCostDto>
{
    public CreateOrderCostValidator()
    {
        RuleFor(x => x.ActualAmount).GreaterThanOrEqualTo(0);
    }
}
