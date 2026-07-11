using FluentValidation;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance.Validators;

public sealed class CreateTicketFundValidator : AbstractValidator<CreateTicketFundDto>
{
    public CreateTicketFundValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
