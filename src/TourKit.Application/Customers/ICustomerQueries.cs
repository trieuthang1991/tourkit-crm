namespace TourKit.Application.Customers;

/// <summary>
/// Query gộp nhiều bảng cho Khách hàng (convention §5 — GROUP BY/COUNT không đủ với IRepository generic).
/// Đẩy phép đếm xuống SQL thay vì materialize toàn bảng (C1). Impl ở Infrastructure (AppDbContext trực tiếp).
/// </summary>
public interface ICustomerQueries
{
    /// <summary>Đếm ở SQL: bao nhiêu KH có ĐÚNG 1 đơn (mua lần đầu) / >1 đơn (mua lại).</summary>
    Task<(int FirstTime, int Repeat)> BuyerCountsAsync();
}
