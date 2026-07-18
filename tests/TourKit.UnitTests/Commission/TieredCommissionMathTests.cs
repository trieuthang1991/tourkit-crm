using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Commission;

/// <summary>
/// Test tra cứu bậc + tính hoa hồng bậc thang (<see cref="TieredCommissionMath"/>) — kiểm biên các mốc.
/// Ba bậc: [0,10tr)=5%, [10tr,50tr)=10%, [50tr,∞)=15% (bậc cuối mở).
/// </summary>
public class TieredCommissionMathTests
{
    private static List<CommissionTier> Bands() =>
    [
        new() { StartAmount = 0m, EndAmount = 10_000_000m, Percentage = 5m },
        new() { StartAmount = 10_000_000m, EndAmount = 50_000_000m, Percentage = 10m },
        new() { StartAmount = 50_000_000m, EndAmount = 999_000_000m, Percentage = 15m },
    ];

    [Theory]
    [InlineData(0, 5)]                 // đúng biên dưới bậc 1 → thuộc bậc 1
    [InlineData(5_000_000, 5)]         // giữa bậc 1
    [InlineData(9_999_999, 5)]         // sát trước mốc → còn bậc 1
    [InlineData(10_000_000, 10)]       // đúng mốc → nhảy sang bậc 2 (StartAmount <= profit)
    [InlineData(49_999_999, 10)]       // sát trước mốc → còn bậc 2
    [InlineData(50_000_000, 15)]       // đúng mốc → bậc 3
    [InlineData(200_000_000, 15)]      // vượt EndAmount bậc cuối → vẫn bậc cuối (mở)
    public void TieredRate_returns_percentage_of_band_containing_profit(decimal profit, decimal expected)
    {
        Assert.Equal(expected, TieredCommissionMath.TieredRate(Bands(), profit));
    }

    [Fact]
    public void TieredRate_below_first_band_is_zero()
    {
        var bands = new List<CommissionTier>
        {
            new() { StartAmount = 10_000_000m, EndAmount = 50_000_000m, Percentage = 10m },
        };
        Assert.Equal(0m, TieredCommissionMath.TieredRate(bands, 5_000_000m));
    }

    [Fact]
    public void TieredRate_no_bands_is_zero()
    {
        Assert.Equal(0m, TieredCommissionMath.TieredRate([], 5_000_000m));
    }

    [Fact]
    public void TieredCommission_multiplies_profit_by_band_rate()
    {
        // 30tr rơi vào bậc 2 (10%) → 3tr
        Assert.Equal(3_000_000m, TieredCommissionMath.TieredCommission(30_000_000m, Bands()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5_000_000)]
    public void TieredCommission_non_positive_profit_is_zero(decimal profit)
    {
        Assert.Equal(0m, TieredCommissionMath.TieredCommission(profit, Bands()));
    }
}
