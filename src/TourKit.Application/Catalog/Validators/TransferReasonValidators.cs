using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateTransferReasonValidator : AbstractValidator<CreateTransferReasonDto>
{
    public CreateTransferReasonValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateTransferReasonValidator : AbstractValidator<UpdateTransferReasonDto>
{
    public UpdateTransferReasonValidator() => RuleFor(x => x.Name).NotEmpty();
}
