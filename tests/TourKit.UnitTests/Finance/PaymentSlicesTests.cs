using Microsoft.EntityFrameworkCore;
using TourKit.Api.Finance.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// Test slice Finance trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>ReceiptSlicesTests</c>).
/// </summary>
public class PaymentSlicesTests
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
        var v = new CreatePaymentValidator();

        Assert.False(v.Validate(new CreatePaymentCommand(
            Guid.NewGuid(), null, null, 0m, "cash", null, null, null)).IsValid);
        Assert.True(v.Validate(new CreatePaymentCommand(
            Guid.NewGuid(), null, null, 1_000_000m, "cash", null, null, null)).IsValid);
    }

    [Fact]
    public async Task CreatePaymentHandler_returns_NotFound_for_missing_order()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreatePaymentHandler(db);

        var result = await handler.Handle(
            new CreatePaymentCommand(Guid.NewGuid(), null, null, 1_000_000m, "cash", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task ApprovePaymentHandler_sets_IsRecognized_true_then_conflicts_on_second_approve()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 5_000_000m };
        db.Orders.Add(order);
        var payment = new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-1", PaymentMethod = "cash",
            OrderId = order.Id, Amount = 1_000_000m, Status = 0, IsRecognized = false,
        };
        db.PaymentVouchers.Add(payment);
        await db.SaveChangesAsync();

        var handler = new ApprovePaymentHandler(db);
        var result = await handler.Handle(new ApprovePaymentCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsRecognized);
        Assert.Equal(1, result.Value.Status);

        var second = await handler.Handle(new ApprovePaymentCommand(payment.Id), CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal(ErrorType.Conflict, second.Error!.Type);
    }
}
