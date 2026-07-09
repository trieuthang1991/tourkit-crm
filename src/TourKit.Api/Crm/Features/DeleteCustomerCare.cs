using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record DeleteCustomerCareCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteCustomerCareHandler : ICommandHandler<DeleteCustomerCareCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteCustomerCareHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteCustomerCareCommand c, CancellationToken ct)
    {
        var care = await _db.CustomerCares.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (care is null)
        {
            return Error.NotFound();
        }

        care.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
