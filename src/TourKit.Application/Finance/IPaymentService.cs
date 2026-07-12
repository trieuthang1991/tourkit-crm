using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(Guid orderId, CreatePaymentDto dto);
    Task<PaymentDto> ApproveAsync(Guid paymentId);
    Task<PaymentDto> RejectAsync(Guid paymentId);
    Task<IReadOnlyList<PaymentDto>> ListByOrderAsync(Guid orderId);
    Task<PagedResult<PaymentListItemDto>> ListAllAsync(int page, int size);
}
