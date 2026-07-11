using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateCustomerTypeValidator : AbstractValidator<CreateCustomerTypeDto>
{
    public CreateCustomerTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Code).GreaterThan(0).WithMessage("Mã loại khách (Code) phải > 0.");
    }
}

public sealed class UpdateCustomerTypeValidator : AbstractValidator<UpdateCustomerTypeDto>
{
    public UpdateCustomerTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Code).GreaterThan(0).WithMessage("Mã loại khách (Code) phải > 0.");
    }
}
