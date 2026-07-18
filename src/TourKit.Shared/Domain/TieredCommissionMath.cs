using TourKit.Shared.Entities;

namespace TourKit.Shared.Domain;

/// <summary>
/// CÔNG THỨC HOA HỒNG BẬC THANG (chính sách theo mốc lợi nhuận) — một chỗ duy nhất.
/// Tách khỏi <see cref="CommissionMath"/> (phẳng, giữ nguyên để tương thích ngược): ở đây % phụ thuộc
/// vào mốc lợi nhuận rơi vào bậc nào của <see cref="CommissionCampaign"/>.
/// </summary>
public static class TieredCommissionMath
{
    /// <summary>
    /// Tìm % của bậc chứa <paramref name="profitAmount"/>: bậc có
    /// <c>StartAmount &lt;= profit &lt; EndAmount</c>. Bậc có StartAmount lớn nhất coi như mở (bỏ chặn trên).
    /// Lợi nhuận dưới bậc thấp nhất, hoặc không có bậc nào → 0 %.
    /// </summary>
    public static decimal TieredRate(IEnumerable<CommissionTier> tiers, decimal profitAmount)
    {
        var ordered = tiers.OrderBy(t => t.StartAmount).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var tier = ordered[i];
            var isLast = i == ordered.Count - 1;
            if (profitAmount >= tier.StartAmount && (isLast || profitAmount < tier.EndAmount))
            {
                return tier.Percentage;
            }
        }

        return 0m;
    }

    /// <summary>
    /// Tiền hoa hồng bậc thang = lợi nhuận × % bậc tương ứng ÷ 100 (làm tròn 2 số).
    /// Lợi nhuận ≤ 0 → 0 (không có lãi thì không hoa hồng).
    /// </summary>
    public static decimal TieredCommission(decimal profit, IEnumerable<CommissionTier> tiers)
        => profit <= 0m ? 0m : Math.Round(profit * TieredRate(tiers, profit) / 100m, 2);
}
