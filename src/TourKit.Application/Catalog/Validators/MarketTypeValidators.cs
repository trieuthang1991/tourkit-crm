using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateMarketTypeValidator : AbstractValidator<CreateMarketTypeDto>
{
    public CreateMarketTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateMarketTypeValidator : AbstractValidator<UpdateMarketTypeDto>
{
    public UpdateMarketTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}
