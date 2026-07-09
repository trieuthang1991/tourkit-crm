using TourKit.Application.Commission.Dtos;

namespace TourKit.Application.Commission;

public interface ICommissionService
{
    Task<OrderProfitDto> GetOrderProfitAsync(Guid orderId);
    Task<ProfitShareDto> CreateProfitShareAsync(Guid orderId, CreateProfitShareDto dto);
    Task<IReadOnlyList<ProfitShareDto>> ListProfitSharesAsync(Guid orderId);
}
