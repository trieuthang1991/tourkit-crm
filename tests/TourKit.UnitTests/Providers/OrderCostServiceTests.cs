using TourKit.Application.Common;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test <see cref="OrderCostService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class OrderCostServiceTests
{
    private static OrderCostService NewService(
        out FakeRepository<OrderCost> repo, out FakeRepository<Order> orderRepo, out FakeRepository<Provider> providerRepo)
    {
        repo = new FakeRepository<OrderCost>();
        orderRepo = new FakeRepository<Order>();
        providerRepo = new FakeRepository<Provider>();
        return new OrderCostService(repo, orderRepo, providerRepo, new CreateOrderCostValidator());
    }

    private static async Task<Order> SeedOrderAsync(FakeRepository<Order> orderRepo)
    {
        var order = new Order { Code = "OD-1", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();
        return order;
    }

    private static async Task<Provider> SeedProviderAsync(FakeRepository<Provider> providerRepo)
    {
        var provider = new Provider { Code = "NCC-1", Name = "Khách sạn ABC", Type = ProviderType.Hotel, Status = 1 };
        await providerRepo.AddAsync(provider);
        await providerRepo.SaveChangesAsync();
        return provider;
    }

    [Fact]
    public async Task CreateAsync_returns_dto_persists_and_updates_order_total_cost()
    {
        var service = NewService(out var repo, out var orderRepo, out var providerRepo);
        var order = await SeedOrderAsync(orderRepo);
        var provider = await SeedProviderAsync(providerRepo);

        var dto = await service.CreateAsync(order.Id, new CreateOrderCostDto(
            provider.Id, "Phòng khách sạn", 1, 2_000_000m, 2_000_000m, 0m, 0m, 0m, 1));

        Assert.Equal(2_000_000m, dto.ActualAmount);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        var updatedOrder = await orderRepo.GetByIdAsync(order.Id);
        Assert.Equal(2_000_000m, updatedOrder!.TotalCost);
    }

    [Fact]
    public async Task CreateAsync_unknown_order_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out var providerRepo);
        var provider = await SeedProviderAsync(providerRepo);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(), new CreateOrderCostDto(provider.Id, "X", 1, 100_000m, 100_000m, 0m, 0m, 0m, 1)));
    }

    [Fact]
    public async Task CreateAsync_unknown_provider_throws_ValidationAppException()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            order.Id, new CreateOrderCostDto(Guid.NewGuid(), "X", 1, 100_000m, 100_000m, 0m, 0m, 0m, 1)));
    }

    [Fact]
    public async Task CreateAsync_negative_actual_amount_throws_ValidationAppException()
    {
        var service = NewService(out _, out var orderRepo, out var providerRepo);
        var order = await SeedOrderAsync(orderRepo);
        var provider = await SeedProviderAsync(providerRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            order.Id, new CreateOrderCostDto(provider.Id, "X", 1, 100_000m, -1m, 0m, 0m, 0m, 1)));
    }

    [Fact]
    public async Task ListByOrderAsync_unknown_order_returns_empty_list()
    {
        var service = NewService(out _, out _, out _);

        var result = await service.ListByOrderAsync(Guid.NewGuid());

        Assert.Empty(result);
    }
}
