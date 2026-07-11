using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateOrderSurchargeValidator : AbstractValidator<CreateOrderSurchargeDto>
{
    public CreateOrderSurchargeValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CalcType).InclusiveBetween(0, 1);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}
