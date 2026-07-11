using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Booking;

/// <summary>
/// Test <see cref="OrderSurchargeService"/> — trọng tâm bất biến tài chính: phụ thu cộng vào
/// Order.TotalRevenue, % tính trên GỐC bất kể thứ tự, xoá trả lại đúng.
/// </summary>
public class OrderSurchargeServiceTests
{
    private static OrderSurchargeService NewService(
        out FakeRepository<OrderSurcharge> repo, out FakeRepository<Order> orderRepo, out FakeRepository<Surcharge> catalogRepo)
    {
        repo = new FakeRepository<OrderSurcharge>();
        orderRepo = new FakeRepository<Order>();
        catalogRepo = new FakeRepository<Surcharge>();
        return new OrderSurchargeService(repo, orderRepo, catalogRepo, new CreateOrderSurchargeValidator());
    }

    private static async Task<Order> SeedOrderAsync(FakeRepository<Order> orderRepo, decimal revenue)
    {
        var order = new Order { Code = "OD-1", TourDepartureId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), TotalRevenue = revenue };
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();
        return order;
    }

    [Fact]
    public void SurchargeAmount_fixed_and_percent()
    {
        Assert.Equal(500_000m, OrderMath.SurchargeAmount((int)SurchargeCalcType.Fixed, 500_000m, 10_000_000m));
        Assert.Equal(1_000_000m, OrderMath.SurchargeAmount((int)SurchargeCalcType.Percent, 10m, 10_000_000m));
    }

    [Fact]
    public async Task Fixed_surcharge_adds_to_total_revenue()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo, 10_000_000m);

        var dto = await service.CreateAsync(order.Id,
            new CreateOrderSurchargeDto(null, "Phụ thu phòng đơn", (int)SurchargeCalcType.Fixed, 500_000m));

        Assert.Equal(500_000m, dto.Amount);
        Assert.Equal(10_500_000m, (await orderRepo.GetByIdAsync(order.Id))!.TotalRevenue);
    }

    [Fact]
    public async Task Percent_surcharge_uses_base_revenue()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo, 10_000_000m);

        var dto = await service.CreateAsync(order.Id,
            new CreateOrderSurchargeDto(null, "Cao điểm 10%", (int)SurchargeCalcType.Percent, 10m));

        Assert.Equal(1_000_000m, dto.Amount);
        Assert.Equal(11_000_000m, (await orderRepo.GetByIdAsync(order.Id))!.TotalRevenue);
    }

    [Fact]
    public async Task Two_percent_surcharges_both_apply_to_base_not_compounding()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo, 10_000_000m);

        var s1 = await service.CreateAsync(order.Id, new CreateOrderSurchargeDto(null, "P1", (int)SurchargeCalcType.Percent, 10m));
        var s2 = await service.CreateAsync(order.Id, new CreateOrderSurchargeDto(null, "P2", (int)SurchargeCalcType.Percent, 5m));

        // Cả hai % tính trên gốc 10tr: 1tr + 500k (KHÔNG cộng dồn: 5% không tính trên 11tr).
        Assert.Equal(1_000_000m, s1.Amount);
        Assert.Equal(500_000m, s2.Amount);
        Assert.Equal(11_500_000m, (await orderRepo.GetByIdAsync(order.Id))!.TotalRevenue);
    }

    [Fact]
    public async Task Delete_surcharge_restores_total_revenue()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo, 10_000_000m);
        var s1 = await service.CreateAsync(order.Id, new CreateOrderSurchargeDto(null, "P1", (int)SurchargeCalcType.Percent, 10m));
        await service.CreateAsync(order.Id, new CreateOrderSurchargeDto(null, "Fixed", (int)SurchargeCalcType.Fixed, 300_000m));

        await service.DeleteAsync(order.Id, s1.Id);

        // Còn lại đúng: 10tr gốc + 300k = 10.3tr (đã trừ đúng 1tr của P1).
        Assert.Equal(10_300_000m, (await orderRepo.GetByIdAsync(order.Id))!.TotalRevenue);
        Assert.Single(await service.ListByOrderAsync(order.Id));
    }

    [Fact]
    public async Task Create_with_catalog_ref_validates_existence()
    {
        var service = NewService(out _, out var orderRepo, out var catalogRepo);
        var order = await SeedOrderAsync(orderRepo, 5_000_000m);
        var surcharge = new Surcharge { Name = "Phòng đơn", CalcType = (int)SurchargeCalcType.Fixed, DefaultValue = 400_000m };
        await catalogRepo.AddAsync(surcharge);
        await catalogRepo.SaveChangesAsync();

        var ok = await service.CreateAsync(order.Id,
            new CreateOrderSurchargeDto(surcharge.Id, "Phòng đơn", (int)SurchargeCalcType.Fixed, 400_000m));
        Assert.Equal(surcharge.Id, ok.SurchargeId);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(order.Id,
            new CreateOrderSurchargeDto(Guid.NewGuid(), "X", (int)SurchargeCalcType.Fixed, 1m)));
    }

    [Fact]
    public async Task Create_unknown_order_throws_NotFound()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(), new CreateOrderSurchargeDto(null, "X", (int)SurchargeCalcType.Fixed, 1m)));
    }

    [Fact]
    public async Task Delete_line_of_other_order_throws_NotFound()
    {
        var service = NewService(out _, out var orderRepo, out _);
        var order = await SeedOrderAsync(orderRepo, 1_000_000m);
        var s1 = await service.CreateAsync(order.Id, new CreateOrderSurchargeDto(null, "X", (int)SurchargeCalcType.Fixed, 1m));

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid(), s1.Id));
    }
}
