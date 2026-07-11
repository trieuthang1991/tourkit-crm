using TourKit.Shared.Domain;

namespace TourKit.UnitTests.Reports;

/// <summary>Test <see cref="OrderMath.Rate"/> — tỉ lệ KPI an toàn chia-0.</summary>
public class OrderMathRateTests
{
    [Fact]
    public void Rate_returns_fraction()
    {
        Assert.Equal(0.5m, OrderMath.Rate(1, 2));
        Assert.Equal(0.75m, OrderMath.Rate(3, 4));
    }

    [Fact]
    public void Rate_zero_denominator_returns_zero()
    {
        Assert.Equal(0m, OrderMath.Rate(5, 0));
        Assert.Equal(0m, OrderMath.Rate(0, 0));
    }
}
