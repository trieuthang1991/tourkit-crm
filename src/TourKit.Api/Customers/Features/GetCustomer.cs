using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Customers.Features;

public sealed record GetCustomerQuery(Guid Id) : IQuery<CustomerResponse>;

public sealed class GetCustomerHandler : IQueryHandler<GetCustomerQuery, CustomerResponse>
{
    private readonly AppDbContext _db;

    public GetCustomerHandler(AppDbContext db) => _db = db;

    public async Task<Result<CustomerResponse>> Handle(GetCustomerQuery q, CancellationToken ct)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == q.Id)
            .Select(c => new CustomerResponse(c.Id, c.FullName, c.Phone))
            .FirstOrDefaultAsync(ct);

        return customer is null ? Error.NotFound() : customer;
    }
}
