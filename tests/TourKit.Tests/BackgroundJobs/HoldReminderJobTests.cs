using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Api.BackgroundJobs;
using TourKit.Api.Tenancy;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.BackgroundJobs;

/// <summary>
/// Job nhắc hạn giữ chỗ: seed dưới 1 tenant, job chạy KHÔNG tenant (IgnoreQueryFilters + per-tenant save).
/// Chỉ nhắc chỗ ĐANG GIỮ (chưa cọc, chưa huỷ) sắp hết hạn ≤24h, có sales phụ trách; idempotent.
/// </summary>
public sealed class HoldReminderJobTests
{
    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<(string To, string Subject)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            Sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }

    private static TourCustomer Seat(Guid orderId, DateTimeOffset? holdExpiresAt, decimal upfront = 0m, int status = 0) => new()
    {
        OrderId = orderId,
        TourDepartureId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Quantity = 1,
        ReservationCode = "RSV-TEST",
        HoldExpiresAt = holdExpiresAt,
        UpfrontAmount = upfront,
        Status = status,
    };

    [Fact]
    public async Task Sends_only_for_expiring_unpaid_holds_with_sales_and_marks_them()
    {
        var db = nameof(Sends_only_for_expiring_unpaid_holds_with_sales_and_marks_them);
        var tenantId = Guid.NewGuid();
        var soon = DateTimeOffset.UtcNow.AddHours(2);       // trong cửa sổ 24h
        var far = DateTimeOffset.UtcNow.AddHours(48);       // ngoài cửa sổ

        var sales = new User { Email = "sales@tourkit.vn", FullName = "Sales A", PasswordHash = "x", IsActive = true };
        var orderWithSales = new Order { Code = "OD-HOLD", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), SalesUserId = sales.Id };
        var orderNoSales = new Order { Code = "OD-NOSALES", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };

        var dueSeat = Seat(orderWithSales.Id, soon);
        var farSeat = Seat(orderWithSales.Id, far);
        var paidSeat = Seat(orderWithSales.Id, soon, upfront: 500_000m);   // đã cọc → không nhắc
        var noSalesSeat = Seat(orderNoSales.Id, soon);                      // chưa gán sales → bỏ qua

        using (var seed = TestDb.Create(new TestTenantContext { TenantId = tenantId }, db))
        {
            seed.Set<User>().Add(sales);
            seed.Set<Order>().AddRange(orderWithSales, orderNoSales);
            seed.Set<TourCustomer>().AddRange(dueSeat, farSeat, paidSeat, noSalesSeat);
            await seed.SaveChangesAsync();
        }

        var email = new CapturingEmailSender();
        var scope = new AmbientTenantContext();
        using (var jobCtx = TestDb.Create(scope, db))
        {
            await new HoldReminderJob(jobCtx, scope, email, NullLogger<HoldReminderJob>.Instance).RunAsync();
        }

        var one = Assert.Single(email.Sent);
        Assert.Equal("sales@tourkit.vn", one.To);
        Assert.Contains("OD-HOLD", one.Subject);

        using var verify = TestDb.Create(new TestTenantContext { TenantId = tenantId }, db);
        Assert.NotNull((await verify.Set<TourCustomer>().FirstAsync(s => s.Id == dueSeat.Id)).HoldReminderSentAt);
        Assert.Null((await verify.Set<TourCustomer>().FirstAsync(s => s.Id == farSeat.Id)).HoldReminderSentAt);
        Assert.Null((await verify.Set<TourCustomer>().FirstAsync(s => s.Id == noSalesSeat.Id)).HoldReminderSentAt);
    }

    [Fact]
    public async Task Running_twice_does_not_resend()
    {
        var db = nameof(Running_twice_does_not_resend) + "_hold";
        var tenantId = Guid.NewGuid();
        var sales = new User { Email = "sales@tourkit.vn", FullName = "Sales A", PasswordHash = "x", IsActive = true };
        var order = new Order { Code = "OD-1", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), SalesUserId = sales.Id };
        var seat = Seat(order.Id, DateTimeOffset.UtcNow.AddHours(1));

        using (var seed = TestDb.Create(new TestTenantContext { TenantId = tenantId }, db))
        {
            seed.Set<User>().Add(sales);
            seed.Set<Order>().Add(order);
            seed.Set<TourCustomer>().Add(seat);
            await seed.SaveChangesAsync();
        }

        var email = new CapturingEmailSender();
        for (var i = 0; i < 2; i++)
        {
            var scope = new AmbientTenantContext();
            using var jobCtx = TestDb.Create(scope, db);
            await new HoldReminderJob(jobCtx, scope, email, NullLogger<HoldReminderJob>.Instance).RunAsync();
        }

        Assert.Single(email.Sent);
    }
}
