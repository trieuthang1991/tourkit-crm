using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateCustomerTagValidator : AbstractValidator<CreateCustomerTagDto>
{
    public CreateCustomerTagValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateCustomerTagValidator : AbstractValidator<UpdateCustomerTagDto>
{
    public UpdateCustomerTagValidator() => RuleFor(x => x.Name).NotEmpty();
}
