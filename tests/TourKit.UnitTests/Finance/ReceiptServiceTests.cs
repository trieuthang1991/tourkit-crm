using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Finance.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// Test <see cref="ReceiptService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class ReceiptServiceTests
{
    private static ReceiptService NewService(
        out FakeRepository<ReceiptVoucher> receiptRepo, out FakeRepository<Order> orderRepo)
    {
        receiptRepo = new FakeRepository<ReceiptVoucher>();
        orderRepo = new FakeRepository<Order>();
        return new ReceiptService(receiptRepo, orderRepo, new FakeRepository<Customer>(), new CreateReceiptValidator());
    }

    private static async Task<Order> SeedOrderAsync(FakeRepository<Order> orderRepo, decimal revenue = 13_000_000m)
    {
        var order = new Order { Code = "ORD-RCP", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), TotalRevenue = revenue };
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task CreateAsync_persists_pending_receipt_not_recognized()
    {
        var service = NewService(out var receiptRepo, out var orderRepo);
        var order = await SeedOrderAsync(orderRepo);

        var created = await service.CreateAsync(order.Id, new CreateReceiptDto(1_000_000m, "cash", null, null));

        Assert.False(created.IsRecognized);
        Assert.Equal(0, created.Status);
        var stored = await receiptRepo.GetByIdAsync(created.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateAsync_unknown_order_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(), new CreateReceiptDto(1_000_000m, "cash", null, null)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_non_positive_amount_throws_ValidationAppException(decimal amount)
    {
        var service = NewService(out _, out var orderRepo);
        var order = await SeedOrderAsync(orderRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            order.Id, new CreateReceiptDto(amount, "cash", null, null)));
    }

    [Fact]
    public async Task ApproveAsync_sets_IsRecognized_true_then_conflicts_on_second_approve()
    {
        var service = NewService(out _, out var orderRepo);
        var order = await SeedOrderAsync(orderRepo);
        var receipt = await service.CreateAsync(order.Id, new CreateReceiptDto(1_000_000m, "cash", null, null));

        var approved = await service.ApproveAsync(receipt.Id);

        Assert.True(approved.IsRecognized);
        Assert.Equal(1, approved.Status);

        await Assert.ThrowsAsync<ConflictException>(() => service.ApproveAsync(receipt.Id));
    }

    [Fact]
    public async Task ApproveAsync_unknown_receipt_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RejectAsync_marks_receipt_not_recognized()
    {
        var service = NewService(out _, out var orderRepo);
        var order = await SeedOrderAsync(orderRepo);
        var receipt = await service.CreateAsync(order.Id, new CreateReceiptDto(1_000_000m, "cash", null, null));

        var rejected = await service.RejectAsync(receipt.Id);

        Assert.False(rejected.IsRecognized);
        Assert.Equal(2, rejected.Status);
    }

    [Fact]
    public async Task GetBalanceAsync_only_counts_recognized_receipts()
    {
        var service = NewService(out _, out var orderRepo);
        var order = await SeedOrderAsync(orderRepo, 13_000_000m);
        var r1 = await service.CreateAsync(order.Id, new CreateReceiptDto(5_000_000m, "cash", null, null));
        await service.ApproveAsync(r1.Id);
        await service.CreateAsync(order.Id, new CreateReceiptDto(8_000_000m, "cash", null, null));
        // second receipt still pending — not counted

        var balance = await service.GetBalanceAsync(order.Id);

        Assert.Equal(13_000_000m, balance.Total);
        Assert.Equal(5_000_000m, balance.Paid);
        Assert.Equal(8_000_000m, balance.Outstanding);
    }

    [Fact]
    public async Task GetBalanceAsync_unknown_order_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetBalanceAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListByOrderAsync_returns_only_receipts_of_that_order()
    {
        var service = NewService(out _, out var orderRepo);
        var order = await SeedOrderAsync(orderRepo);
        var other = await SeedOrderAsync(orderRepo);
        await service.CreateAsync(order.Id, new CreateReceiptDto(1_000_000m, "cash", null, null));
        await service.CreateAsync(other.Id, new CreateReceiptDto(2_000_000m, "cash", null, null));

        var receipts = await service.ListByOrderAsync(order.Id);

        var single = Assert.Single(receipts);
        Assert.Equal(order.Id, single.OrderId);
    }
}
