using FluentValidation;
using TourKit.Application.Marketing.Dtos;

namespace TourKit.Application.Marketing.Validators;

public sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignDto>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class UpdateCampaignValidator : AbstractValidator<UpdateCampaignDto>
{
    public UpdateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}
