using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record DeleteProviderCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteProviderHandler : ICommandHandler<DeleteProviderCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteProviderHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteProviderCommand c, CancellationToken ct)
    {
        var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Id == c.Id, ct);
        if (provider is null)
        {
            return Error.NotFound();
        }

        provider.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
