using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateTourTemplateValidator : AbstractValidator<CreateTourTemplateDto>
{
    public CreateTourTemplateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalSlots).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservationHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceAdult).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceChild).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceChildSmall).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceBaby).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateTourTemplateValidator : AbstractValidator<UpdateTourTemplateDto>
{
    public UpdateTourTemplateValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalSlots).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservationHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceAdult).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceChild).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceChildSmall).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceBaby).GreaterThanOrEqualTo(0);
    }
}
