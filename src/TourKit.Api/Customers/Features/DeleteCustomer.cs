using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Customers.Features;

public sealed record DeleteCustomerCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteCustomerHandler : ICommandHandler<DeleteCustomerCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteCustomerHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteCustomerCommand c, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (customer is null)
        {
            return Error.NotFound();
        }

        customer.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
