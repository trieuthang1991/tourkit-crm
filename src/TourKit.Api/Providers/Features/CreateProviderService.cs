using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record CreateProviderServiceCommand(
    Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status) : ICommand<ProviderServiceResponse>;

public sealed class CreateProviderServiceValidator : AbstractValidator<CreateProviderServiceCommand>
{
    public CreateProviderServiceValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.PriceName).MaximumLength(200);
    }
}

public sealed class CreateProviderServiceHandler : ICommandHandler<CreateProviderServiceCommand, ProviderServiceResponse>
{
    private readonly AppDbContext _db;

    public CreateProviderServiceHandler(AppDbContext db) => _db = db;

    public async Task<Result<ProviderServiceResponse>> Handle(CreateProviderServiceCommand c, CancellationToken ct)
    {
        if (!await _db.Providers.AnyAsync(p => p.Id == c.ProviderId, ct))
        {
            return Error.Validation($"Nhà cung cấp '{c.ProviderId}' không tồn tại.");
        }

        var providerService = new ProviderService
        {
            ProviderId = c.ProviderId,
            ServiceItemId = c.ServiceItemId,
            PriceName = c.PriceName,
            ContractPrice = c.ContractPrice,
            PublicPrice = c.PublicPrice,
            AmountOfPeople = c.AmountOfPeople,
            Note = c.Note,
            Status = c.Status,
        };
        _db.ProviderServices.Add(providerService);
        await _db.SaveChangesAsync(ct);

        return new ProviderServiceResponse(
            providerService.Id, providerService.ProviderId, providerService.ServiceItemId, providerService.PriceName,
            providerService.ContractPrice, providerService.PublicPrice, providerService.AmountOfPeople,
            providerService.Note, providerService.Status);
    }
}
