using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record DeleteProviderServiceCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteProviderServiceHandler : ICommandHandler<DeleteProviderServiceCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteProviderServiceHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteProviderServiceCommand c, CancellationToken ct)
    {
        var providerService = await _db.ProviderServices.FirstOrDefaultAsync(p => p.Id == c.Id, ct);
        if (providerService is null)
        {
            return Error.NotFound();
        }

        providerService.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
