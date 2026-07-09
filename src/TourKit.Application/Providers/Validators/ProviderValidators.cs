using FluentValidation;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers.Validators;

public sealed class CreateProviderValidator : AbstractValidator<CreateProviderDto>
{
    public CreateProviderValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateProviderValidator : AbstractValidator<UpdateProviderDto>
{
    public UpdateProviderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
