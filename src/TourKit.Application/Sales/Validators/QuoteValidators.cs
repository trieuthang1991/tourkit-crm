using FluentValidation;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales.Validators;

public sealed class CreateQuoteValidator : AbstractValidator<CreateQuoteDto>
{
    public CreateQuoteValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        QuoteRules.ApplyPricingHeaderRules(this);
        RuleForEach(x => x.Lines).ChildRules(QuoteRules.ApplyLineRules);
    }
}

public sealed class UpdateQuoteValidator : AbstractValidator<UpdateQuoteDto>
{
    public UpdateQuoteValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        QuoteRules.ApplyPricingHeaderRules(this);
        RuleForEach(x => x.Lines).ChildRules(QuoteRules.ApplyLineRules);
    }
}

/// <summary>Rule dùng chung Create/Update — ràng buộc dự trù giá (spec 2026-07-11).</summary>
internal static class QuoteRules
{
    public static void ApplyPricingHeaderRules(AbstractValidator<CreateQuoteDto> v)
    {
        v.RuleFor(x => x.Adults).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.Infants).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.ChildPercent).InclusiveBetween(0, 100);
        v.RuleFor(x => x.InfantPercent).InclusiveBetween(0, 100);
    }

    public static void ApplyPricingHeaderRules(AbstractValidator<UpdateQuoteDto> v)
    {
        v.RuleFor(x => x.Adults).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.Infants).GreaterThanOrEqualTo(0);
        v.RuleFor(x => x.ChildPercent).InclusiveBetween(0, 100);
        v.RuleFor(x => x.InfantPercent).InclusiveBetween(0, 100);
    }

    public static void ApplyLineRules(InlineValidator<CreateQuoteLineDto> line)
    {
        line.RuleFor(l => l.Description).NotEmpty();
        line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(0);
        line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        line.RuleFor(l => l.MarginPercent).InclusiveBetween(0, 500);
    }
}
