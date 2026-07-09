namespace TourKit.Shared.Domain;

/// <summary>
/// CÔNG THỨC HOA HỒNG / CHIA LỢI NHUẬN — một chỗ duy nhất.
/// Dùng cho ProfitShare (chia lợi nhuận) và báo cáo hoa hồng theo NV.
/// </summary>
public static class CommissionMath
{
    /// <summary>
    /// Tiền hoa hồng/chia = lợi nhuận nền × phần trăm ÷ 100 (làm tròn 2 số).
    /// Lợi nhuận ≤ 0 → 0 (không chia/không hoa hồng khi đơn không có lãi).
    /// </summary>
    public static decimal ShareAmount(decimal profitBase, decimal percentage)
        => profitBase <= 0m ? 0m : Math.Round(profitBase * percentage / 100m, 2);
}
