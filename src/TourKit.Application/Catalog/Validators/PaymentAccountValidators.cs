using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreatePaymentAccountValidator : AbstractValidator<CreatePaymentAccountDto>
{
    public CreatePaymentAccountValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdatePaymentAccountValidator : AbstractValidator<UpdatePaymentAccountDto>
{
    public UpdatePaymentAccountValidator() => RuleFor(x => x.Name).NotEmpty();
}
