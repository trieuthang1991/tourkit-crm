using FluentValidation;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking.Validators;

public sealed class CreateServicePaymentTermValidator : AbstractValidator<CreateServicePaymentTermDto>
{
    public CreateServicePaymentTermValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền đợt chi phải > 0.");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).InclusiveBetween(0, 1);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class UpdateServicePaymentTermValidator : AbstractValidator<UpdateServicePaymentTermDto>
{
    public UpdateServicePaymentTermValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền đợt chi phải > 0.");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).InclusiveBetween(0, 1);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
