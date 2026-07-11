using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;
// Tránh nhầm với class service TourKit.Application.Providers.ProviderService cùng namespace.
using ProviderServiceEntity = TourKit.Shared.Entities.ProviderService;

namespace TourKit.Application.Providers;

/// <summary>
/// Chi phí trả NCC theo đơn (Order_Chi hệ cũ). Mỗi lần thêm chi phí, Order.TotalCost được recompute lại
/// từ toàn bộ dòng chi phí (công thức duy nhất ở <see cref="OrderMath.TotalCost"/>) và lưu chung
/// 1 SaveChanges với dòng chi phí mới.
/// </summary>
public sealed class OrderCostService(
    IRepository<OrderCost> repo,
    IRepository<Order> orderRepo,
    IRepository<Provider> providerRepo,
    IRepository<ProviderServiceEntity> providerServiceRepo,
    IValidator<CreateOrderCostDto> createValidator) : IOrderCostService
{
    public async Task<IReadOnlyList<OrderCostDto>> ListByOrderAsync(Guid orderId)
    {
        var costs = await repo.ListAsync(c => c.OrderId == orderId);
        return costs.OrderBy(c => c.DayIndex).Select(Map).ToList();
    }

    public async Task<OrderCostDto> CreateAsync(Guid orderId, CreateOrderCostDto dto)
    {
        await Validate(createValidator, dto);

        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        if (!await providerRepo.AnyAsync(p => p.Id == dto.ProviderId))
        {
            throw new ValidationAppException("Nhà cung cấp không tồn tại.");
        }

        // Nếu chọn giá từ bảng giá NCC: dòng giá phải tồn tại và thuộc đúng NCC của chi phí này.
        if (dto.ProviderServiceId is { } priceId
            && !await providerServiceRepo.AnyAsync(s => s.Id == priceId && s.ProviderId == dto.ProviderId))
        {
            throw new ValidationAppException("Bảng giá không tồn tại hoặc không thuộc nhà cung cấp đã chọn.");
        }

        var cost = new OrderCost
        {
            OrderId = orderId,
            ProviderId = dto.ProviderId,
            ProviderServiceId = dto.ProviderServiceId,
            ServiceName = dto.ServiceName,
            DayIndex = dto.DayIndex,
            ExpectedAmount = dto.ExpectedAmount,
            ActualAmount = dto.ActualAmount,
            Deposit = dto.Deposit,
            Surcharge = dto.Surcharge,
            Vat = dto.Vat,
            Status = dto.Status,
        };
        await repo.AddAsync(cost);

        // Recompute Order.TotalCost = tổng ActualAmount toàn bộ dòng chi phí của đơn (kể cả dòng mới).
        var existingCosts = await repo.ListAsync(c => c.OrderId == orderId);
        order.TotalCost = OrderMath.TotalCost(existingCosts.Append(cost));
        orderRepo.Update(order);

        await repo.SaveChangesAsync();

        return Map(cost);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static OrderCostDto Map(OrderCost c) => new(
        c.Id, c.OrderId, c.ProviderId, c.ProviderServiceId, c.ServiceName, c.DayIndex,
        c.ExpectedAmount, c.ActualAmount, c.Deposit, c.Surcharge, c.Vat, c.Status);
}
