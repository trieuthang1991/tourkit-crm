using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateServiceBookingValidator : AbstractValidator<CreateServiceBookingDto>
{
    public CreateServiceBookingValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        // Nếu có cả 2 ngày: EndDate >= StartDate.
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Ngày kết thúc phải >= ngày bắt đầu.");
    }
}

public sealed class UpdateServiceBookingValidator : AbstractValidator<UpdateServiceBookingDto>
{
    public UpdateServiceBookingValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Ngày kết thúc phải >= ngày bắt đầu.");
    }
}
