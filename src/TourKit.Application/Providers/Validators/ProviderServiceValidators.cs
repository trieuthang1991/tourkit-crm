using FluentValidation;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers.Validators;

public sealed class CreateProviderServiceValidator : AbstractValidator<CreateProviderServiceDto>
{
    public CreateProviderServiceValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.PriceName).MaximumLength(200);
    }
}

public sealed class UpdateProviderServiceValidator : AbstractValidator<UpdateProviderServiceDto>
{
    public UpdateProviderServiceValidator()
    {
        RuleFor(x => x.PriceName).MaximumLength(200);
    }
}
