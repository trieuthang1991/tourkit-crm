using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Commission.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Commission;

/// <summary>
/// Test <see cref="CommissionService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class CommissionServiceTests
{
    private static CommissionService NewService(
        out FakeRepository<Order> orderRepo, out FakeRepository<OrderCost> costRepo, out FakeRepository<ProfitShare> shareRepo)
    {
        orderRepo = new FakeRepository<Order>();
        costRepo = new FakeRepository<OrderCost>();
        shareRepo = new FakeRepository<ProfitShare>();
        return new CommissionService(orderRepo, costRepo, shareRepo, new CreateProfitShareValidator());
    }

    private static async Task<Order> SeedOrderAsync(FakeRepository<Order> orderRepo, decimal revenue = 13_000_000m)
    {
        var order = new Order
        {
            Code = "ORD-COMM", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), TotalRevenue = revenue,
        };
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();
        return order;
    }

    private static async Task SeedCostAsync(FakeRepository<OrderCost> costRepo, Guid orderId, decimal actualAmount)
    {
        await costRepo.AddAsync(new OrderCost
        {
            OrderId = orderId, ProviderId = Guid.NewGuid(), DayIndex = 1, ExpectedAmount = actualAmount, ActualAmount = actualAmount,
        });
        await costRepo.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOrderProfitAsync_returns_revenue_cost_and_profit()
    {
        var service = NewService(out var orderRepo, out var costRepo, out _);
        var order = await SeedOrderAsync(orderRepo);
        await SeedCostAsync(costRepo, order.Id, 3_000_000m);

        var profit = await service.GetOrderProfitAsync(order.Id);

        Assert.Equal(13_000_000m, profit.Revenue);
        Assert.Equal(3_000_000m, profit.Cost);
        Assert.Equal(10_000_000m, profit.Profit);
    }

    [Fact]
    public async Task GetOrderProfitAsync_unknown_order_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetOrderProfitAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateProfitShareAsync_computes_amount_from_order_profit()
    {
        var service = NewService(out var orderRepo, out var costRepo, out var shareRepo);
        var order = await SeedOrderAsync(orderRepo);
        await SeedCostAsync(costRepo, order.Id, 3_000_000m);

        var share = await service.CreateProfitShareAsync(order.Id, new CreateProfitShareDto(Guid.NewGuid(), 10m));

        Assert.Equal(10_000_000m, share.ProfitBase);
        Assert.Equal(1_000_000m, share.Amount);
        var stored = await shareRepo.GetByIdAsync(share.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateProfitShareAsync_unknown_order_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateProfitShareAsync(
            Guid.NewGuid(), new CreateProfitShareDto(Guid.NewGuid(), 10m)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task CreateProfitShareAsync_invalid_percentage_throws_ValidationAppException(decimal percentage)
    {
        var service = NewService(out var orderRepo, out _, out _);
        var order = await SeedOrderAsync(orderRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateProfitShareAsync(
            order.Id, new CreateProfitShareDto(Guid.NewGuid(), percentage)));
    }

    [Fact]
    public async Task ListProfitSharesAsync_unknown_order_returns_empty_list()
    {
        var service = NewService(out _, out _, out _);

        var result = await service.ListProfitSharesAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListProfitSharesAsync_returns_shares_created_for_order()
    {
        var service = NewService(out var orderRepo, out var costRepo, out _);
        var order = await SeedOrderAsync(orderRepo);
        await SeedCostAsync(costRepo, order.Id, 3_000_000m);
        var created = await service.CreateProfitShareAsync(order.Id, new CreateProfitShareDto(Guid.NewGuid(), 10m));

        var shares = await service.ListProfitSharesAsync(order.Id);

        var single = Assert.Single(shares);
        Assert.Equal(created.Id, single.Id);
    }
}
