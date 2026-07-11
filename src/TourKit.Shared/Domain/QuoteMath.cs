using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Shared.Domain;

/// <summary>Kết quả tính giá dự trù của 1 báo giá (spec 2026-07-11-quote-cost-estimation).</summary>
public sealed record QuotePricing(
    decimal TotalCost, decimal TotalAmount, decimal TotalProfit,
    decimal AdultPrice, decimal ChildPrice, decimal InfantPrice);

/// <summary>
/// CÔNG THỨC DỰ TRÙ GIÁ TOUR — một chỗ duy nhất (mirror <see cref="OrderMath"/>).
/// Nghiệp vụ bám legacy DuTruTours/BaoGia: %lợi nhuận theo dòng (percent_loi_nhuan_khach),
/// giá trẻ em/trẻ nhỏ = % giá người lớn (percent_price_tre_em/tre_nho), lợi nhuận dự kiến
/// (loi_nhuan_du_kien). Chi phí đoàn (PerGroup) chia theo số khách QUY ĐỔI để
/// TotalAmount = AdultPrice×NL + ChildPrice×TE + InfantPrice×TN khớp tuyệt đối.
/// </summary>
public static class QuoteMath
{
    /// <summary>Giá bán đơn vị của 1 dòng: có giá vốn thì = vốn × (1+%LN); vốn = 0 giữ giá nhập tay (báo giá nhanh).</summary>
    public static decimal UnitSellPrice(decimal unitCost, decimal marginPercent, decimal manualUnitPrice)
        => unitCost > 0 ? unitCost * (1 + marginPercent / 100m) : manualUnitPrice;

    /// <summary>Số khách quy đổi: NL + TE×%TE + TN×%TN (hệ số trùng với hệ số giá hạng khách).</summary>
    public static decimal PaxEquivalent(int adults, int children, int infants, decimal childPercent, decimal infantPercent)
        => adults + children * childPercent / 100m + infants * infantPercent / 100m;

    /// <summary>Tính toàn bộ giá dự trù từ các dòng đã có UnitPrice (giá bán đơn vị) chốt.</summary>
    public static QuotePricing Price(
        IEnumerable<QuoteLine> lines, int adults, int children, int infants,
        decimal childPercent, decimal infantPercent)
    {
        decimal perPaxCost = 0, perPaxSell = 0, groupCost = 0, groupSell = 0;
        foreach (var l in lines)
        {
            var cost = l.Quantity * l.UnitCost;
            var sell = l.Quantity * l.UnitPrice;
            if (l.Scope == (int)QuoteLineScope.PerPerson)
            {
                perPaxCost += cost;
                perPaxSell += sell;
            }
            else
            {
                groupCost += cost;
                groupSell += sell;
            }
        }

        var paxEq = PaxEquivalent(adults, children, infants, childPercent, infantPercent);
        var totalCost = perPaxCost * paxEq + groupCost;
        var totalAmount = perPaxSell * paxEq + groupSell;
        var adultPrice = perPaxSell + (paxEq > 0 ? groupSell / paxEq : 0);

        return new QuotePricing(
            totalCost, totalAmount, totalAmount - totalCost,
            adultPrice, adultPrice * childPercent / 100m, adultPrice * infantPercent / 100m);
    }
}
