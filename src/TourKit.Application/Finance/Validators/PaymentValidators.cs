using FluentValidation;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance.Validators;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0.");
    }
}
