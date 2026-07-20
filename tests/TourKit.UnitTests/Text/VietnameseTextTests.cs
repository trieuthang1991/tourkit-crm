using TourKit.Shared.Text;

namespace TourKit.UnitTests.Text;

public class VietnameseTextTests
{
    [Theory]
    [InlineData("Nguyễn Văn Ân", "nguyen van an")]
    [InlineData("Đặng THỊ Yến", "dang thi yen")]
    [InlineData("  Hồ Chí Minh ", "ho chi minh")]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeSearch_removes_diacritics_and_lowercases(string? input, string? expected)
        => Assert.Equal(expected, VietnameseText.NormalizeSearch(input));

    [Theory]
    [InlineData("+84 901 234-567", "0901234567")]
    [InlineData("0901234567", "0901234567")]
    [InlineData("84901234567", "0901234567")]
    [InlineData("(090) 123.4567", "0901234567")]
    [InlineData(null, "")]
    public void NormalizePhone_strips_and_normalizes_prefix(string? input, string expected)
        => Assert.Equal(expected, VietnameseText.NormalizePhone(input));
}
