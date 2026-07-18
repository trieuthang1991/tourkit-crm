namespace TourKit.Application.Booking.Dtos;

/// <summary>Một đợt trong lịch thanh toán NCC của booking dịch vụ (legacy ServicePaymentTerm).</summary>
public sealed record ServicePaymentTermDto(
    Guid Id, Guid ServiceBookingId, Guid? OrderCostId, decimal Amount, DateTimeOffset DueDate,
    string? Note, int Status, decimal PaidAmount, decimal RemainingAmount, Guid? PaymentVoucherId);

/// <summary>Thêm một đợt chi vào lịch của booking.</summary>
public sealed record CreateServicePaymentTermDto(
    decimal Amount, DateTimeOffset DueDate, string? Note = null,
    Guid? OrderCostId = null, decimal PaidAmount = 0, int Status = 0, Guid? PaymentVoucherId = null);

/// <summary>Cập nhật một đợt chi (số tiền/hạn/ghi chú/trạng thái/đã chi).</summary>
public sealed record UpdateServicePaymentTermDto(
    decimal Amount, DateTimeOffset DueDate, string? Note, int Status, decimal PaidAmount,
    Guid? OrderCostId = null, Guid? PaymentVoucherId = null);

/// <summary>
/// Cảnh báo hạn chi NCC cho dashboard điều hành/tài chính: đợt sắp đến hạn (trong N ngày tới)
/// hoặc đã quá hạn (DueDate &lt; hôm nay, Status = chờ chi).
/// </summary>
public sealed record ServicePaymentTermAlertDto(
    Guid Id, Guid ServiceBookingId, string ServiceCode, string? ProviderName,
    decimal Amount, decimal PaidAmount, decimal RemainingAmount,
    DateTimeOffset DueDate, int DaysUntilDue, bool IsOverdue, string? Note);
