using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateCarTypeValidator : AbstractValidator<CreateCarTypeDto>
{
    public CreateCarTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Code).GreaterThan(0).WithMessage("Số ghế (Code) phải > 0.");
    }
}

public sealed class UpdateCarTypeValidator : AbstractValidator<UpdateCarTypeDto>
{
    public UpdateCarTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Code).GreaterThan(0).WithMessage("Số ghế (Code) phải > 0.");
    }
}
