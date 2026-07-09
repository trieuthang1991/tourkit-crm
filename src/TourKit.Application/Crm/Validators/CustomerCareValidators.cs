using FluentValidation;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm.Validators;

public sealed class CreateCustomerCareValidator : AbstractValidator<CreateCustomerCareDto>
{
    public CreateCustomerCareValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateCustomerCareValidator : AbstractValidator<UpdateCustomerCareDto>
{
    public UpdateCustomerCareValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
    }
}
