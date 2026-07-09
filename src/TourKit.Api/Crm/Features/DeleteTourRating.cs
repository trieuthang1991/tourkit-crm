using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record DeleteTourRatingCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteTourRatingHandler : ICommandHandler<DeleteTourRatingCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteTourRatingHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteTourRatingCommand c, CancellationToken ct)
    {
        var rating = await _db.TourRatings.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (rating is null)
        {
            return Error.NotFound();
        }

        rating.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
