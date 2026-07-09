using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record UpdateProviderServiceCommand(
    Guid Id, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status) : ICommand<bool>;

public sealed class UpdateProviderServiceValidator : AbstractValidator<UpdateProviderServiceCommand>
{
    public UpdateProviderServiceValidator()
    {
        RuleFor(x => x.PriceName).MaximumLength(200);
    }
}

public sealed class UpdateProviderServiceHandler : ICommandHandler<UpdateProviderServiceCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateProviderServiceHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateProviderServiceCommand c, CancellationToken ct)
    {
        var providerService = await _db.ProviderServices.FirstOrDefaultAsync(p => p.Id == c.Id, ct);
        if (providerService is null)
        {
            return Error.NotFound();
        }

        providerService.ServiceItemId = c.ServiceItemId;
        providerService.PriceName = c.PriceName;
        providerService.ContractPrice = c.ContractPrice;
        providerService.PublicPrice = c.PublicPrice;
        providerService.AmountOfPeople = c.AmountOfPeople;
        providerService.Note = c.Note;
        providerService.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
