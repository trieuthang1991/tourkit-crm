using FluentValidation;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers.Validators;

public sealed class CreateServiceItemValidator : AbstractValidator<CreateServiceItemDto>
{
    public CreateServiceItemValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateServiceItemValidator : AbstractValidator<UpdateServiceItemDto>
{
    public UpdateServiceItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
