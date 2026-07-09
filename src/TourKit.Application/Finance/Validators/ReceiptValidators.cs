using FluentValidation;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance.Validators;

public sealed class CreateReceiptValidator : AbstractValidator<CreateReceiptDto>
{
    public CreateReceiptValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0.");
    }
}
