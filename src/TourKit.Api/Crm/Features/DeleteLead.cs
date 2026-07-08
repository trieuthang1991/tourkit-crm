using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record DeleteLeadCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteLeadHandler : ICommandHandler<DeleteLeadCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteLeadHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteLeadCommand c, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (lead is null)
        {
            return Error.NotFound();
        }

        lead.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
