using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class CreatePositionValidator : AbstractValidator<CreatePositionDto>
{
    public CreatePositionValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdatePositionValidator : AbstractValidator<UpdatePositionDto>
{
    public UpdatePositionValidator() => RuleFor(x => x.Name).NotEmpty();
}
