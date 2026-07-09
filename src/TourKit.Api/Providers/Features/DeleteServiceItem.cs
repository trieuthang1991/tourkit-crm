using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record DeleteServiceItemCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteServiceItemHandler : ICommandHandler<DeleteServiceItemCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteServiceItemHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteServiceItemCommand c, CancellationToken ct)
    {
        var serviceItem = await _db.ServiceItems.FirstOrDefaultAsync(s => s.Id == c.Id, ct);
        if (serviceItem is null)
        {
            return Error.NotFound();
        }

        serviceItem.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
