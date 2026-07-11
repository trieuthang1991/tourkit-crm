using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Phụ thu theo đơn (legacy SurchargeServices). Thành tiền cộng thẳng vào Order.TotalRevenue nên tự
/// chảy vào công nợ/hoa hồng/báo cáo. Bất biến: TotalRevenue = giá gốc + Σ Amount → % tính trên GỐC
/// (base = TotalRevenue − Σ Amount hiện có) bất kể thứ tự thêm; xoá thì trừ đúng Amount đã lưu.
/// </summary>
public sealed class OrderSurchargeService(
    IRepository<OrderSurcharge> repo,
    IRepository<Order> orderRepo,
    IRepository<Surcharge> surchargeRepo,
    IValidator<CreateOrderSurchargeDto> createValidator) : IOrderSurchargeService
{
    public async Task<IReadOnlyList<OrderSurchargeDto>> ListByOrderAsync(Guid orderId)
    {
        var items = await repo.ListAsync(x => x.OrderId == orderId);
        return items.OrderBy(x => x.CreatedAt).Select(Map).ToList();
    }

    public async Task<OrderSurchargeDto> CreateAsync(Guid orderId, CreateOrderSurchargeDto dto)
    {
        await Validate(createValidator, dto);

        var order = await orderRepo.GetByIdAsync(orderId) ?? throw new NotFoundException();

        if (dto.SurchargeId is { } sid && !await surchargeRepo.AnyAsync(s => s.Id == sid))
        {
            throw new ValidationAppException("Loại phụ thu không tồn tại.");
        }

        // Giá gốc = doanh thu hiện tại trừ tổng phụ thu đã có → % luôn tính trên gốc.
        var existing = await repo.ListAsync(x => x.OrderId == orderId);
        var baseRevenue = order.TotalRevenue - existing.Sum(x => x.Amount);
        var amount = OrderMath.SurchargeAmount(dto.CalcType, dto.Value, baseRevenue);

        var line = new OrderSurcharge
        {
            OrderId = orderId,
            SurchargeId = dto.SurchargeId,
            Description = dto.Description.Trim(),
            CalcType = dto.CalcType,
            Value = dto.Value,
            Amount = amount,
        };
        await repo.AddAsync(line);

        order.TotalRevenue += amount;
        orderRepo.Update(order);

        await repo.SaveChangesAsync();
        await orderRepo.SaveChangesAsync();

        return Map(line);
    }

    public async Task DeleteAsync(Guid orderId, Guid surchargeLineId)
    {
        var line = await repo.GetByIdAsync(surchargeLineId);
        if (line is null || line.OrderId != orderId)
        {
            throw new NotFoundException();
        }

        var order = await orderRepo.GetByIdAsync(orderId) ?? throw new NotFoundException();
        order.TotalRevenue -= line.Amount;   // trừ đúng thành tiền đã lưu → giữ bất biến
        orderRepo.Update(order);

        repo.Remove(line);
        await repo.SaveChangesAsync();
        await orderRepo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static OrderSurchargeDto Map(OrderSurcharge x) => new(
        x.Id, x.OrderId, x.SurchargeId, x.Description, x.CalcType, x.Value, x.Amount);
}
