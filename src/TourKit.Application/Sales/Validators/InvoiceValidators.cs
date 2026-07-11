using FluentValidation;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales.Validators;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.BuyerName).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.VatRate).InclusiveBetween(0m, 100m);
        });
    }
}

public sealed class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceDto>
{
    public UpdateInvoiceValidator()
    {
        RuleFor(x => x.BuyerName).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.VatRate).InclusiveBetween(0m, 100m);
        });
    }
}
