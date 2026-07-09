using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers;

public interface IOrderCostService
{
    Task<IReadOnlyList<OrderCostDto>> ListByOrderAsync(Guid orderId);
    Task<OrderCostDto> CreateAsync(Guid orderId, CreateOrderCostDto dto);
}
