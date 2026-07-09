using FluentValidation;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm.Validators;

public sealed class CreateTourRatingValidator : AbstractValidator<CreateTourRatingDto>
{
    public CreateTourRatingValidator()
    {
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
    }
}

public sealed class UpdateTourRatingValidator : AbstractValidator<UpdateTourRatingDto>
{
    public UpdateTourRatingValidator()
    {
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
    }
}
