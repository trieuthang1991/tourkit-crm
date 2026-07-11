using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreatePaymentTermValidator : AbstractValidator<CreatePaymentTermDto>
{
    public CreatePaymentTermValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdatePaymentTermValidator : AbstractValidator<UpdatePaymentTermDto>
{
    public UpdatePaymentTermValidator() => RuleFor(x => x.Name).NotEmpty();
}
