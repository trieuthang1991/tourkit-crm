namespace TourKit.Shared.Enums;

/// <summary>Trạng thái yêu cầu báo giá của Đại lý (B2B Portal MVP §4.2.3): hỏi giá → chào giá → xác nhận/từ chối.</summary>
public enum AgentQuoteStatus
{
    Requested = 1,   // Đại lý gửi yêu cầu
    Quoted = 2,      // Sales đã chào giá
    Confirmed = 3,   // Đại lý xác nhận (điều kiện tạo Booking)
    Rejected = 4,    // Từ chối
}
