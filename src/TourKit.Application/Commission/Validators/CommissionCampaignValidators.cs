using FluentValidation;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Application.Commission.Validators;

public sealed class CreateCommissionCampaignValidator : AbstractValidator<CreateCommissionCampaignDto>
{
    public CreateCommissionCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên chính sách không được trống.").MaximumLength(250);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        RuleForEach(x => x.Tiers).SetValidator(new CommissionTierInputValidator());
    }
}

public sealed class UpdateCommissionCampaignValidator : AbstractValidator<UpdateCommissionCampaignDto>
{
    public UpdateCommissionCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên chính sách không được trống.").MaximumLength(250);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        RuleForEach(x => x.Tiers).SetValidator(new CommissionTierInputValidator());
    }
}

public sealed class CommissionTierInputValidator : AbstractValidator<CommissionTierInputDto>
{
    public CommissionTierInputValidator()
    {
        RuleFor(x => x.StartAmount).GreaterThanOrEqualTo(0).WithMessage("Mốc bắt đầu phải ≥ 0.");
        RuleFor(x => x.EndAmount).GreaterThanOrEqualTo(x => x.StartAmount)
            .WithMessage("Mốc kết thúc phải ≥ mốc bắt đầu.");
        RuleFor(x => x.Percentage).GreaterThanOrEqualTo(0).WithMessage("Tỉ lệ hoa hồng phải ≥ 0.");
    }
}
