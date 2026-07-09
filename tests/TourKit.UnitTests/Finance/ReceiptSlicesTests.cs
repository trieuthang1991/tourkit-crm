using Microsoft.EntityFrameworkCore;
using TourKit.Api.Finance.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// Test slice Finance trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>).
/// </summary>
public class ReceiptSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    [Fact]
    public void Validator_rejects_zero_amount()
    {
        var v = new CreateReceiptValidator();

        Assert.False(v.Validate(new CreateReceiptCommand(Guid.NewGuid(), 0m, "cash", null, null)).IsValid);
        Assert.True(v.Validate(new CreateReceiptCommand(Guid.NewGuid(), 1_000_000m, "cash", null, null)).IsValid);
    }

    [Fact]
    public async Task CreateReceiptHandler_returns_NotFound_for_missing_order()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateReceiptHandler(db);

        var result = await handler.Handle(
            new CreateReceiptCommand(Guid.NewGuid(), 1_000_000m, "cash", null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task ApproveReceiptHandler_sets_IsRecognized_true()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 5_000_000m };
        db.Orders.Add(order);
        var receipt = new ReceiptVoucher
        {
            TenantId = tenant.TenantId, Code = "RCP-1", PaymentMethod = "cash",
            OrderId = order.Id, Amount = 1_000_000m, Status = 0, IsRecognized = false,
        };
        db.ReceiptVouchers.Add(receipt);
        await db.SaveChangesAsync();

        var handler = new ApproveReceiptHandler(db);
        var result = await handler.Handle(new ApproveReceiptCommand(receipt.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsRecognized);
        Assert.Equal(1, result.Value.Status);
    }
}
