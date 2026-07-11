using FluentValidation;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Application.B2B.Validators;

public sealed class CreateAgentValidator : AbstractValidator<CreateAgentDto>
{
    public CreateAgentValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateAgentValidator : AbstractValidator<UpdateAgentDto>
{
    public UpdateAgentValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateAgentQuoteRequestValidator : AbstractValidator<CreateAgentQuoteRequestDto>
{
    public CreateAgentQuoteRequestValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PaxCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.TravelDate!.Value)
            .When(x => x.TravelDate.HasValue && x.ReturnDate.HasValue)
            .WithMessage("Ngày về phải >= ngày đi.");
    }
}
