using FluentValidation;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Application.Flights.Validators;

public sealed class CreateFlightTicketValidator : AbstractValidator<CreateFlightTicketDto>
{
    public CreateFlightTicketValidator()
    {
        RuleFor(x => x.Pnr).NotEmpty().WithMessage("PNR bắt buộc.").MaximumLength(50);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Days).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservedAmount).GreaterThanOrEqualTo(0);
    }
}
