using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateBranchValidator : AbstractValidator<CreateBranchDto>
{
    public CreateBranchValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchDto>
{
    public UpdateBranchValidator() => RuleFor(x => x.Name).NotEmpty();
}
