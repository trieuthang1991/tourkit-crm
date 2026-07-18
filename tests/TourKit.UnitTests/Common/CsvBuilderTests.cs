using System.Text;
using TourKit.Application.Common;

namespace TourKit.UnitTests.Common;

/// <summary>Test <see cref="CsvBuilder"/> — BOM + escape RFC4180 + ghép dòng.</summary>
public class CsvBuilderTests
{
    [Fact]
    public void Build_co_BOM_header_va_dong()
    {
        var bytes = CsvBuilder.Build(["Tên", "SĐT"], [["An", "0901"], ["Bình", "0902"]]);

        // BOM UTF-8 ở đầu.
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var text = Encoding.UTF8.GetString(bytes)[1..]; // bỏ ký tự BOM
        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Assert.Equal("Tên,SĐT", lines[0]);
        Assert.Equal("An,0901", lines[1]);
        Assert.Equal("Bình,0902", lines[2]);
    }

    [Theory]
    [InlineData("a,b", "\"a,b\"")]        // có phẩy → bọc ngoặc
    [InlineData("x\"y", "\"x\"\"y\"")]    // có ngoặc kép → nhân đôi
    [InlineData("dòng\nmới", "\"dòng\nmới\"")] // xuống dòng → bọc
    [InlineData("bình thường", "bình thường")] // không đặc biệt → giữ nguyên
    public void EscapeCell_theo_rfc4180(string input, string expected)
    {
        Assert.Equal(expected, CsvBuilder.EscapeCell(input));
    }
}
