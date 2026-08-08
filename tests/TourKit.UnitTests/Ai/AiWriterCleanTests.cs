using TourKit.Ai;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Dọn văn bản model sinh ra. Nội dung này đi thẳng ra màn hình và có khi được chép sang tin nhắn gửi
/// khách, nên dấu markdown hay lời dẫn thừa sót lại là người dùng phải tự xoá bằng tay.
/// </summary>
public class AiWriterCleanTests
{
    [Fact]
    public void Bo_dau_markdown()
    {
        Assert.Equal("Khách quan tâm tour Đà Nẵng.",
            AiWriterService.Clean("**Khách quan tâm** tour Đà Nẵng."));
    }

    [Fact]
    public void Bo_tieu_de_markdown()
    {
        Assert.Equal("Nội dung", AiWriterService.Clean("### Nội dung"));
    }

    /// <summary>Model hay mở đầu bằng "Đây là tin nhắn:" dù prompt đã cấm.</summary>
    [Fact]
    public void Bo_loi_dan_o_dong_dau()
    {
        var input = "Đây là tin nhắn gửi khách:\nEm chào anh Hà, bên em có tour Đà Nẵng ạ.";

        Assert.Equal("Em chào anh Hà, bên em có tour Đà Nẵng ạ.", AiWriterService.Clean(input));
    }

    /// <summary>Một dòng duy nhất kết thúc bằng dấu hai chấm là nội dung thật — không được cắt.</summary>
    [Fact]
    public void Khong_cat_khi_chi_co_mot_dong()
    {
        Assert.Equal("Cần làm tiếp:", AiWriterService.Clean("Cần làm tiếp:"));
    }

    /// <summary>Dòng đầu dài thì đó là nội dung chứ không phải lời dẫn.</summary>
    [Fact]
    public void Khong_cat_dong_dau_qua_dai()
    {
        var input = new string('a', 90) + ":\ndòng hai";

        Assert.StartsWith("aaa", AiWriterService.Clean(input), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rong_thi_tra_null(string? text)
    {
        Assert.Null(AiWriterService.Clean(text));
    }

    [Fact]
    public void Giu_nguyen_xuong_dong()
    {
        Assert.Equal("một\nhai", AiWriterService.Clean("một\nhai"));
    }
}
