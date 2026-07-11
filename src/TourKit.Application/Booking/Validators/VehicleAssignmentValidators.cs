using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateVehicleAssignmentValidator : AbstractValidator<CreateVehicleAssignmentDto>
{
    public CreateVehicleAssignmentValidator()
    {
        RuleFor(x => x.TourDepartureId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        // Legacy: TimeCome (trả) phải >= TimeGo (đón).
        RuleFor(x => x.TimeCome)
            .GreaterThanOrEqualTo(x => x.TimeGo!.Value)
            .When(x => x.TimeGo.HasValue && x.TimeCome.HasValue)
            .WithMessage("Giờ trả (TimeCome) phải >= giờ đón (TimeGo).");
    }
}

public sealed class UpdateVehicleAssignmentValidator : AbstractValidator<UpdateVehicleAssignmentDto>
{
    public UpdateVehicleAssignmentValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.TimeCome)
            .GreaterThanOrEqualTo(x => x.TimeGo!.Value)
            .When(x => x.TimeGo.HasValue && x.TimeCome.HasValue)
            .WithMessage("Giờ trả (TimeCome) phải >= giờ đón (TimeGo).");
    }
}
