using TourKit.Shared.Domain;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Sales;

/// <summary>Công thức dự trù giá (spec 2026-07-11) — pure function, test số liệu cụ thể.</summary>
public sealed class QuoteMathTests
{
    private static QuoteLine Line(int scope, int qty, decimal cost, decimal margin, decimal? manualPrice = null) => new()
    {
        Scope = scope,
        Quantity = qty,
        UnitCost = cost,
        MarginPercent = margin,
        UnitPrice = QuoteMath.UnitSellPrice(cost, margin, manualPrice ?? 0m),
    };

    [Fact]
    public void UnitSellPrice_from_cost_and_margin()
    {
        Assert.Equal(1_200_000m, QuoteMath.UnitSellPrice(1_000_000m, 20m, 0m));
    }

    [Fact]
    public void UnitSellPrice_keeps_manual_price_when_no_cost()
    {
        // Báo giá nhanh kiểu cũ: không nhập vốn, giữ giá gõ tay.
        Assert.Equal(5_000_000m, QuoteMath.UnitSellPrice(0m, 20m, 5_000_000m));
    }

    [Fact]
    public void Group_only_quote_reproduces_legacy_totals()
    {
        // Backward-compat: 0 khách + dòng PerGroup → TotalAmount = Σ qty×price y hệt Quote cũ.
        var lines = new[] { Line((int)QuoteLineScope.PerGroup, 2, 0m, 0m, 5_000_000m) };

        var p = QuoteMath.Price(lines, 0, 0, 0, 75m, 50m);

        Assert.Equal(10_000_000m, p.TotalAmount);
        Assert.Equal(0m, p.TotalCost);
        Assert.Equal(10_000_000m, p.TotalProfit);
        Assert.Equal(0m, p.AdultPrice); // paxEq=0 → không có giá đầu khách
    }

    [Fact]
    public void Mixed_quote_prices_match_legacy_formula()
    {
        // Đoàn 10 NL + 2 TE(75%) + 1 TN(50%): paxEq = 10 + 1.5 + 0.5 = 12.
        // PerPerson: phòng 3 đêm × vốn 500k, LN 20% → bán 600k/đêm → 1.8tr/khách.
        // PerGroup: xe 4 ngày × vốn 3tr, LN 10% → bán 3.3tr/ngày → 13.2tr cả đoàn.
        var lines = new[]
        {
            Line((int)QuoteLineScope.PerPerson, 3, 500_000m, 20m),
            Line((int)QuoteLineScope.PerGroup, 4, 3_000_000m, 10m),
        };

        var p = QuoteMath.Price(lines, 10, 2, 1, 75m, 50m);

        Assert.Equal(12m, QuoteMath.PaxEquivalent(10, 2, 1, 75m, 50m));
        Assert.Equal(3 * 500_000m * 12 + 4 * 3_000_000m, p.TotalCost);        // 18tr + 12tr = 30tr
        Assert.Equal(3 * 600_000m * 12 + 4 * 3_300_000m, p.TotalAmount);      // 21.6tr + 13.2tr = 34.8tr
        Assert.Equal(4_800_000m, p.TotalProfit);
        Assert.Equal(1_800_000m + 13_200_000m / 12, p.AdultPrice);            // 2.9tr/NL
        Assert.Equal(p.AdultPrice * 0.75m, p.ChildPrice);
        Assert.Equal(p.AdultPrice * 0.5m, p.InfantPrice);
        // Nhất quán: tổng bán = Σ giá hạng × số khách hạng.
        Assert.Equal(p.TotalAmount, p.AdultPrice * 10 + p.ChildPrice * 2 + p.InfantPrice * 1);
    }
}
