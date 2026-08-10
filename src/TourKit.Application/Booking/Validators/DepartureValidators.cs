using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateDepartureValidator : AbstractValidator<CreateDepartureDto>
{
    public CreateDepartureValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
    }
}

public sealed class UpdateDepartureValidator : AbstractValidator<UpdateDepartureDto>
{
    public UpdateDepartureValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
    }
}
