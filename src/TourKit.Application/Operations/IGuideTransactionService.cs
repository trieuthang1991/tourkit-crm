namespace TourKit.Application.Operations;

public interface IGuideTransactionService
{
    Task<GuideSettlementDto> GetByAssignmentAsync(Guid assignmentId);
    Task<GuideTransactionDto> CreateAsync(Guid assignmentId, CreateGuideTransactionDto dto);
    Task DeleteAsync(Guid assignmentId, Guid transactionId);
}
