using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateCurrencyValidator : AbstractValidator<CreateCurrencyDto>
{
    public CreateCurrencyValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.RateToVnd).GreaterThan(0).WithMessage("Tỷ giá phải > 0.");
    }
}

public sealed class UpdateCurrencyValidator : AbstractValidator<UpdateCurrencyDto>
{
    public UpdateCurrencyValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.RateToVnd).GreaterThan(0).WithMessage("Tỷ giá phải > 0.");
    }
}
