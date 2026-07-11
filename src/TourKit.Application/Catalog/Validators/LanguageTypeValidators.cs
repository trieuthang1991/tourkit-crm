using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateLanguageTypeValidator : AbstractValidator<CreateLanguageTypeDto>
{
    public CreateLanguageTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateLanguageTypeValidator : AbstractValidator<UpdateLanguageTypeDto>
{
    public UpdateLanguageTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}
