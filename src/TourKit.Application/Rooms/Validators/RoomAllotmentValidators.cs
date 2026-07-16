using FluentValidation;
using TourKit.Application.Rooms.Dtos;

namespace TourKit.Application.Rooms.Validators;

public sealed class CreateRoomAllotmentValidator : AbstractValidator<CreateRoomAllotmentDto>
{
    public CreateRoomAllotmentValidator()
    {
        RuleFor(x => x.ProviderRef).NotEmpty().WithMessage("Nhà cung cấp bắt buộc.").MaximumLength(100);
        RuleFor(x => x.ServiceName).NotEmpty().WithMessage("Tên dịch vụ/loại phòng bắt buộc.").MaximumLength(200);
        RuleFor(x => x.Quota).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Booked).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateRoomAllotmentValidator : AbstractValidator<UpdateRoomAllotmentDto>
{
    public UpdateRoomAllotmentValidator()
    {
        RuleFor(x => x.ProviderRef).NotEmpty().WithMessage("Nhà cung cấp bắt buộc.").MaximumLength(100);
        RuleFor(x => x.ServiceName).NotEmpty().WithMessage("Tên dịch vụ/loại phòng bắt buộc.").MaximumLength(200);
        RuleFor(x => x.Quota).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Booked).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
