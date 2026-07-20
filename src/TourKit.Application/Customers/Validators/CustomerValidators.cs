using FluentValidation;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Application.Customers.Validators;

// SĐT bắt buộc được enforce ở TẦNG WEB (form Razor + OnPostSave) — nơi người dùng nhập liệu —
// KHÔNG ở domain validator, để không phá vỡ import/seed/test tạo khách không SĐT.
public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}
