using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance;

public interface IReceiptApprovalService
{
    Task<ApprovalDto> StartAsync(Guid receiptId, StartApprovalDto dto);
    Task<ApprovalDto> ActAsync(Guid receiptId, Guid userId, ActApprovalDto dto);
    Task<ApprovalDto> GetAsync(Guid receiptId);
}
