using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record CreateServiceItemCommand(string Code, string Name, int Category, int Status)
    : ICommand<ServiceItemResponse>;

public sealed class CreateServiceItemValidator : AbstractValidator<CreateServiceItemCommand>
{
    public CreateServiceItemValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class CreateServiceItemHandler : ICommandHandler<CreateServiceItemCommand, ServiceItemResponse>
{
    private readonly AppDbContext _db;

    public CreateServiceItemHandler(AppDbContext db) => _db = db;

    public async Task<Result<ServiceItemResponse>> Handle(CreateServiceItemCommand c, CancellationToken ct)
    {
        var code = c.Code.Trim();
        if (await _db.ServiceItems.AnyAsync(s => s.Code == code, ct))
        {
            return Error.Conflict($"Mã dịch vụ '{code}' đã tồn tại.");
        }

        var serviceItem = new ServiceItem
        {
            Code = code,
            Name = c.Name.Trim(),
            Category = c.Category,
            Status = c.Status,
        };
        _db.ServiceItems.Add(serviceItem);
        await _db.SaveChangesAsync(ct);

        return new ServiceItemResponse(
            serviceItem.Id, serviceItem.Code, serviceItem.Name, serviceItem.Category, serviceItem.Status);
    }
}
