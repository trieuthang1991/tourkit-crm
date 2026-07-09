using Microsoft.EntityFrameworkCore;
using TourKit.Api.Crm.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test slice Lead trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>).
/// </summary>
public class LeadSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateLeadCommand Valid() => new("Nguyen Van A", "0900000000", "a@x.com", "facebook", null);

    [Fact]
    public void Validator_rejects_empty_full_name()
    {
        var v = new CreateLeadValidator();

        Assert.False(v.Validate(Valid() with { FullName = "" }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public async Task ConvertLeadHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new ConvertLeadHandler(db);

        var result = await handler.Handle(new ConvertLeadCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task ConvertLeadHandler_returns_Conflict_when_already_converted()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var lead = new Lead
        {
            TenantId = tenant.TenantId, FullName = "Nguyen Van B",
            Status = LeadStatus.Won, ConvertedCustomerId = Guid.NewGuid(),
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var handler = new ConvertLeadHandler(db);
        var result = await handler.Handle(new ConvertLeadCommand(lead.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }
}
