namespace TourKit.Shared.Enums;

/// <summary>Trạng thái chỗ (suy ra từ upfront_amount vs giá + timeRemaining — theo flow "Giữ chỗ" hệ cũ).</summary>
public enum SeatStatus
{
    Held = 1,           // giữ chỗ, còn đếm ngược (HoldExpiresAt != null, upfront = 0)
    HeldConfirmed = 2,  // chốt chỗ, không nhả (đã xác nhận chỗ → HoldExpiresAt = null, upfront = 0)
    Deposited = 3,      // đã đặt cọc (0 < upfront < tổng giá dòng)
    Paid = 4,           // đã thanh toán (upfront >= tổng giá dòng)
    Cancelled = 5,      // đã huỷ chỗ (statusCancel != 0 hệ cũ)
}
