namespace TourKit.Shared.Domain;

/// <summary>
/// CÔNG THỨC HOÀN TIỀN KHI HUỶ CHỖ — một chỗ duy nhất (legacy CancelSeats).
/// </summary>
public static class RefundMath
{
    /// <summary>Tiền còn giữ lại (không hoàn) = đã cọc − số hoàn.</summary>
    public static decimal Remain(decimal upfront, decimal refund) => upfront - refund;

    /// <summary>% hoàn trên số đã cọc (làm tròn 2 số); chưa cọc → 0.</summary>
    public static decimal Percentage(decimal upfront, decimal refund)
        => upfront > 0m ? Math.Round(refund / upfront * 100m, 2) : 0m;
}
