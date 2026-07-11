using FluentValidation;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog.Validators;

public sealed class CreateRoomClassValidator : AbstractValidator<CreateRoomClassDto>
{
    public CreateRoomClassValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateRoomClassValidator : AbstractValidator<UpdateRoomClassDto>
{
    public UpdateRoomClassValidator() => RuleFor(x => x.Name).NotEmpty();
}
