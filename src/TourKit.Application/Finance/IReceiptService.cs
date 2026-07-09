using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance;

public interface IReceiptService
{
    Task<ReceiptDto> CreateAsync(Guid orderId, CreateReceiptDto dto);
    Task<ReceiptDto> ApproveAsync(Guid receiptId);
    Task<ReceiptDto> RejectAsync(Guid receiptId);
    Task<IReadOnlyList<ReceiptDto>> ListByOrderAsync(Guid orderId);
    Task<OrderBalanceDto> GetBalanceAsync(Guid orderId);
}
