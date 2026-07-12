using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateTourGroupValidator : AbstractValidator<CreateTourGroupDto>
{
    public CreateTourGroupValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateTourGroupValidator : AbstractValidator<UpdateTourGroupDto>
{
    public UpdateTourGroupValidator() => RuleFor(x => x.Name).NotEmpty();
}
