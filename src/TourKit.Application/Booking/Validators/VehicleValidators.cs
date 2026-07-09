using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleDto>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateVehicleValidator : AbstractValidator<UpdateVehicleDto>
{
    public UpdateVehicleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
