using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Finance.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// Test <see cref="PaymentService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class PaymentServiceTests
{
    private static PaymentService NewService(
        out FakeRepository<PaymentVoucher> paymentRepo, out FakeRepository<Order> orderRepo, out FakeRepository<Provider> providerRepo)
    {
        paymentRepo = new FakeRepository<PaymentVoucher>();
        orderRepo = new FakeRepository<Order>();
        providerRepo = new FakeRepository<Provider>();
        return new PaymentService(paymentRepo, orderRepo, providerRepo, new CreatePaymentValidator());
    }

    private static async Task<Order> SeedOrderAsync(FakeRepository<Order> orderRepo, decimal revenue = 13_000_000m)
    {
        var order = new Order { Code = "ORD-PAY", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), TotalRevenue = revenue };
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task CreateAsync_persists_pending_payment_not_recognized()
    {
        var service = NewService(out var paymentRepo, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);

        var created = await service.CreateAsync(order.Id, new CreatePaymentDto(null, null, 1_000_000m, "cash", null, null, null));

        Assert.False(created.IsRecognized);
        Assert.Equal(0, created.Status);
        var stored = await paymentRepo.GetByIdAsync(created.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateAsync_unknown_order_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(), new CreatePaymentDto(null, null, 1_000_000m, "cash", null, null, null)));
    }

    [Fact]
    public async Task CreateAsync_unknown_provider_throws_ValidationAppException()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            order.Id, new CreatePaymentDto(Guid.NewGuid(), null, 1_000_000m, "cash", null, null, null)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_non_positive_amount_throws_ValidationAppException(decimal amount)
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            order.Id, new CreatePaymentDto(null, null, amount, "cash", null, null, null)));
    }

    [Fact]
    public async Task ApproveAsync_sets_IsRecognized_true_then_conflicts_on_second_approve()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);
        var payment = await service.CreateAsync(order.Id, new CreatePaymentDto(null, null, 1_000_000m, "cash", null, null, null));

        var approved = await service.ApproveAsync(payment.Id);

        Assert.True(approved.IsRecognized);
        Assert.Equal(1, approved.Status);

        await Assert.ThrowsAsync<ConflictException>(() => service.ApproveAsync(payment.Id));
    }

    [Fact]
    public async Task RejectAsync_marks_payment_not_recognized_then_conflicts_on_second_reject()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);
        var payment = await service.CreateAsync(order.Id, new CreatePaymentDto(null, null, 1_000_000m, "cash", null, null, null));

        var rejected = await service.RejectAsync(payment.Id);

        Assert.False(rejected.IsRecognized);
        Assert.Equal(2, rejected.Status);

        await Assert.ThrowsAsync<ConflictException>(() => service.RejectAsync(payment.Id));
    }

    [Fact]
    public async Task ListByOrderAsync_returns_only_payments_of_that_order()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);
        var other = await SeedOrderAsync(orderRepo);
        await service.CreateAsync(order.Id, new CreatePaymentDto(null, null, 1_000_000m, "cash", null, null, null));
        await service.CreateAsync(other.Id, new CreatePaymentDto(null, null, 2_000_000m, "cash", null, null, null));

        var payments = await service.ListByOrderAsync(order.Id);

        var single = Assert.Single(payments);
        Assert.Equal(order.Id, single.OrderId);
    }

    [Fact]
    public async Task ListAllAsync_filters_by_status_and_q_plus_stats()
    {
        var service = NewService(out var paymentRepo, out _, out _);
        await paymentRepo.AddAsync(new PaymentVoucher { Code = "PC1", OrderId = Guid.NewGuid(), Amount = 1_000_000m, PaymentMethod = "cash", Status = 0 });
        await paymentRepo.AddAsync(new PaymentVoucher { Code = "PC2", OrderId = Guid.NewGuid(), Amount = 2_000_000m, PaymentMethod = "cash", Status = 1 });
        await paymentRepo.SaveChangesAsync();

        Assert.Equal("PC1", Assert.Single((await service.ListAllAsync(1, 20, new PaymentListFilter(Status: 0))).Items).Code);
        Assert.Equal("PC2", Assert.Single((await service.ListAllAsync(1, 20, new PaymentListFilter(Q: "PC2"))).Items).Code);
        Assert.Equal("PC2", Assert.Single((await service.ListAllAsync(1, 20, new PaymentListFilter(AmountFrom: 1_500_000m))).Items).Code);
        Assert.Equal(2, (await service.ListAllAsync(1, 20, new PaymentListFilter(PaymentMethod: "cash"))).Items.Count);

        var stats = await service.GetStatsAsync();
        Assert.Equal(2, stats.Total);
        Assert.Equal(3_000_000m, stats.TotalAmount);
        Assert.Equal(1, stats.Pending);
        Assert.Equal(1, stats.Approved);
    }
}
