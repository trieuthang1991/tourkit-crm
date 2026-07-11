using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateGuideAssignmentValidator : AbstractValidator<CreateGuideAssignmentDto>
{
    public CreateGuideAssignmentValidator()
    {
        RuleFor(x => x.TourDepartureId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        // Legacy: TimeCome (kết thúc) phải >= TimeGo (khởi hành).
        RuleFor(x => x.TimeCome)
            .GreaterThanOrEqualTo(x => x.TimeGo!.Value)
            .When(x => x.TimeGo.HasValue && x.TimeCome.HasValue)
            .WithMessage("Giờ kết thúc (TimeCome) phải >= giờ khởi hành (TimeGo).");
    }
}

public sealed class UpdateGuideAssignmentValidator : AbstractValidator<UpdateGuideAssignmentDto>
{
    public UpdateGuideAssignmentValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.TimeCome)
            .GreaterThanOrEqualTo(x => x.TimeGo!.Value)
            .When(x => x.TimeGo.HasValue && x.TimeCome.HasValue)
            .WithMessage("Giờ kết thúc (TimeCome) phải >= giờ khởi hành (TimeGo).");
    }
}
