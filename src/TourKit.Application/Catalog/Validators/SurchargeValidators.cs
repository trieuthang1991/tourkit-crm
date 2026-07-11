using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateSurchargeValidator : AbstractValidator<CreateSurchargeDto>
{
    public CreateSurchargeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.CalcType).InclusiveBetween(0, 1);
        RuleFor(x => x.DefaultValue).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSurchargeValidator : AbstractValidator<UpdateSurchargeDto>
{
    public UpdateSurchargeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.CalcType).InclusiveBetween(0, 1);
        RuleFor(x => x.DefaultValue).GreaterThanOrEqualTo(0);
    }
}
