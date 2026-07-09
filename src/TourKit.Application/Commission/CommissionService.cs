using FluentValidation;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Commission;

/// <summary>
/// Hoa hồng/chia lợi nhuận theo đơn (ProfitSharing hệ cũ). Lợi nhuận đơn = doanh thu − chi phí, chi phí
/// = Σ ActualAmount toàn bộ dòng OrderCost của đơn — tính duy nhất qua <see cref="OrderMath"/>.
/// </summary>
public sealed class CommissionService(
    IRepository<Order> orderRepo,
    IRepository<OrderCost> costRepo,
    IRepository<ProfitShare> shareRepo,
    IValidator<CreateProfitShareDto> createShareValidator) : ICommissionService
{
    public async Task<OrderProfitDto> GetOrderProfitAsync(Guid orderId)
    {
        var (revenue, cost, profit) = await ComputeProfitAsync(orderId);
        return new OrderProfitDto(revenue, cost, profit);
    }

    public async Task<ProfitShareDto> CreateProfitShareAsync(Guid orderId, CreateProfitShareDto dto)
    {
        await Validate(createShareValidator, dto);

        var (_, _, profit) = await ComputeProfitAsync(orderId);
        var amount = CommissionMath.ShareAmount(profit, dto.Percentage);

        var share = new ProfitShare
        {
            OrderId = orderId,
            UserId = dto.UserId,
            Percentage = dto.Percentage,
            Amount = amount,
            ProfitBase = profit,
        };
        await shareRepo.AddAsync(share);
        await shareRepo.SaveChangesAsync();

        return Map(share);
    }

    public async Task<IReadOnlyList<ProfitShareDto>> ListProfitSharesAsync(Guid orderId)
    {
        var shares = await shareRepo.ListAsync(s => s.OrderId == orderId);
        return shares.Select(Map).ToList();
    }

    private async Task<(decimal Revenue, decimal Cost, decimal Profit)> ComputeProfitAsync(Guid orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        var costs = await costRepo.ListAsync(c => c.OrderId == orderId);
        var cost = OrderMath.TotalCost(costs);
        var profit = OrderMath.Profit(order.TotalRevenue, cost);

        return (order.TotalRevenue, cost, profit);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static ProfitShareDto Map(ProfitShare s) => new(
        s.Id, s.OrderId, s.UserId, s.Percentage, s.Amount, s.ProfitBase);
}
