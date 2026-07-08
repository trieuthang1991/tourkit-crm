using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record ConvertLeadCommand(Guid Id) : ICommand<ConvertLeadResponse>;

public sealed class ConvertLeadHandler : ICommandHandler<ConvertLeadCommand, ConvertLeadResponse>
{
    private readonly AppDbContext _db;

    public ConvertLeadHandler(AppDbContext db) => _db = db;

    public async Task<Result<ConvertLeadResponse>> Handle(ConvertLeadCommand c, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (lead is null)
        {
            return Error.NotFound();
        }

        if (lead.ConvertedCustomerId is not null)
        {
            return Error.Conflict("Lead đã được convert.");
        }

        var customer = new Customer { FullName = lead.FullName, Phone = lead.Phone };
        _db.Customers.Add(customer);
        lead.Status = LeadStatus.Won;
        lead.ConvertedCustomerId = customer.Id;
        await _db.SaveChangesAsync(ct);

        return new ConvertLeadResponse(customer.Id);
    }
}
