using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Domain;

/// <summary>Quy tắc tài chính dùng chung (một chỗ) — đổi ở đây thì balance + báo cáo đều theo.</summary>
public static class ReceiptQueries
{
    /// <summary>Phiếu thu được TÍNH vào "đã thu"/công nợ: đã duyệt (IsRecognized). Đổi quy tắc ở ĐÂY.</summary>
    public static IQueryable<ReceiptVoucher> Recognized(this IQueryable<ReceiptVoucher> query) =>
        query.Where(r => r.IsRecognized);
}
