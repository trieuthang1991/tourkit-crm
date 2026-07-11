using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateCustomerSourceValidator : AbstractValidator<CreateCustomerSourceDto>
{
    public CreateCustomerSourceValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateCustomerSourceValidator : AbstractValidator<UpdateCustomerSourceDto>
{
    public UpdateCustomerSourceValidator() => RuleFor(x => x.Name).NotEmpty();
}
