using FluentValidation;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Application.Flights.Validators;

public sealed class CreateFlightTicketIndividualValidator : AbstractValidator<CreateFlightTicketIndividualDto>
{
    public CreateFlightTicketIndividualValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Mã vé bắt buộc.").MaximumLength(50);
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Tên khách bắt buộc.").MaximumLength(200);
        RuleFor(x => x.SellAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateFlightTicketIndividualValidator : AbstractValidator<UpdateFlightTicketIndividualDto>
{
    public UpdateFlightTicketIndividualValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Mã vé bắt buộc.").MaximumLength(50);
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Tên khách bắt buộc.").MaximumLength(200);
        RuleFor(x => x.SellAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
    }
}
