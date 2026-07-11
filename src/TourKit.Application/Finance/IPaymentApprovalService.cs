using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance;

public interface IPaymentApprovalService
{
    Task<PaymentApprovalDto> StartAsync(Guid paymentId, StartApprovalDto dto);
    Task<PaymentApprovalDto> ActAsync(Guid paymentId, Guid userId, ActApprovalDto dto);
    Task<PaymentApprovalDto> GetAsync(Guid paymentId);
}
