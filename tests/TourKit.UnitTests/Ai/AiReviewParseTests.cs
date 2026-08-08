using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Ghép kết quả model với bộ tiêu chí và TÍNH điểm tổng.
///
/// Đây là chỗ dễ vỡ nhất: model có thể bọc JSON trong khối mã, bỏ sót tiêu chí, trả điểm ngoài thang,
/// hoặc trả một đoạn văn. Mọi trường hợp phải cho ra hoặc một nhận định đúng, hoặc null — tuyệt đối
/// không phải một điểm số trông hợp lý mà sai.
/// </summary>
public class AiReviewParseTests
{
    private static AiScoringProfile Profile()
    {
        var p = new AiScoringProfile();
        p.Criteria.Add(new AiScoreCriterion("nhu_cau", "Nhu cầu", 50, "..."));
        p.Criteria.Add(new AiScoreCriterion("tuong_tac", "Tương tác", 30, "..."));
        p.Criteria.Add(new AiScoreCriterion("lien_he", "Liên hệ", 20, "..."));
        return p;
    }

    private static readonly IReadOnlyList<AiScoreBand> Bands =
        [new(80, "Nóng"), new(50, "Ấm"), new(0, "Nguội")];

    private static AiReview? Run(string? text) => AiReviewService.Combine(text, Profile(), Bands);

    /// <summary>Điểm tổng = trung bình có trọng số. 100×50% + 50×30% + 0×20% = 65.</summary>
    [Fact]
    public void Diem_tong_tinh_theo_trong_so_chu_khong_lay_cua_model()
    {
        var review = Run("""
            {"criteria":[{"key":"nhu_cau","score":100,"note":"a"},{"key":"tuong_tac","score":50,"note":"b"},
            {"key":"lien_he","score":0,"note":"c"}],"summary":"x","score":999}
            """);

        Assert.Equal(65, review!.Score);
        Assert.Equal("Ấm", review.Band);
        Assert.Equal(3, review.Criteria.Count);
    }

    /// <summary>
    /// Model bỏ sót tiêu chí thì tính 0 và giữ nguyên mẫu số. Bỏ tiêu chí ra khỏi phép tính sẽ làm
    /// điểm tổng TĂNG đúng vào lúc model làm việc kém nhất — hệt như thưởng cho việc trả lời thiếu.
    /// </summary>
    [Fact]
    public void Tieu_chi_bi_bo_sot_tinh_0_chu_khong_bi_loai_khoi_mau_so()
    {
        var review = Run("""{"criteria":[{"key":"nhu_cau","score":100,"note":"a"}],"summary":"x"}""");

        Assert.Equal(50, review!.Score);   // 100×50% + 0 + 0
        Assert.Equal(3, review.Criteria.Count);
        Assert.Contains(review.Criteria, c => c.Key == "lien_he" && c.Score == 0);
        Assert.Contains(review.Criteria, c => c.Note.Contains("không chấm", StringComparison.Ordinal));
    }

    [Fact]
    public void Diem_tieu_chi_ngoai_thang_bi_kep_lai()
    {
        var review = Run("""
            {"criteria":[{"key":"nhu_cau","score":500},{"key":"tuong_tac","score":-20},
            {"key":"lien_he","score":100}],"summary":"x"}
            """);

        Assert.Equal(100, review!.Criteria.First(c => c.Key == "nhu_cau").Score);
        Assert.Equal(0, review.Criteria.First(c => c.Key == "tuong_tac").Score);
        Assert.Equal(70, review.Score);   // 100×50% + 0×30% + 100×20%
    }

    /// <summary>Model hay bọc trong ```json dù prompt đã cấm — không vì thế mà bỏ cả nhận định.</summary>
    [Fact]
    public void Doc_duoc_ca_khi_bi_boc_trong_khoi_ma()
    {
        var review = Run("""
            Đây là kết quả:
            ```json
            {"criteria":[{"key":"nhu_cau","score":80}],"summary":"ok"}
            ```
            """);

        Assert.Equal(40, review!.Score);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Khách này khá tiềm năng.")]
    [InlineData("{ khong phai json }")]
    public void Khong_doc_duoc_thi_tra_null_chu_khong_dung_nhan_dinh_rong(string? text)
    {
        Assert.Null(Run(text));
    }

    /// <summary>Không có tóm tắt thì nhận định vô dụng — thà báo không đánh giá được.</summary>
    [Fact]
    public void Thieu_tom_tat_thi_coi_nhu_that_bai()
    {
        Assert.Null(Run("""{"criteria":[{"key":"nhu_cau","score":90}]}"""));
    }

    [Theory]
    [InlineData(100, "Nóng")]
    [InlineData(80, "Nóng")]
    [InlineData(79, "Ấm")]
    [InlineData(50, "Ấm")]
    [InlineData(0, "Nguội")]
    public void Nhan_lay_tu_thang_trong_cau_hinh(int score, string expected)
    {
        Assert.Equal(expected, AiReviewService.BandOf(score, Bands));
    }

    /// <summary>Đổi ngưỡng trong cấu hình phải đổi được nhãn — đó là mục đích của việc đưa ra JSON.</summary>
    [Fact]
    public void Doi_thang_trong_cau_hinh_thi_nhan_doi_theo()
    {
        IReadOnlyList<AiScoreBand> strict = [new(95, "Xuất sắc"), new(0, "Bình thường")];

        Assert.Equal("Bình thường", AiReviewService.BandOf(90, strict));
        Assert.Equal("Xuất sắc", AiReviewService.BandOf(95, strict));
    }

    [Fact]
    public void Khong_khai_thang_thi_dung_thang_mac_dinh()
    {
        Assert.Equal("Nóng", AiReviewService.BandOf(90, null));
        Assert.Equal("Nguội", AiReviewService.BandOf(10, []));
    }

    [Fact]
    public void Danh_sach_qua_dai_bi_cat_va_bo_dong_rong()
    {
        var items = string.Join(",", Enumerable.Range(1, 20).Select(i => $"\"y{i}\""));
        var review = Run($$"""{"criteria":[],"summary":"x","risks":[{{items}},"","   "]}""");

        Assert.Equal(8, review!.Risks.Count);
        Assert.DoesNotContain("", review.Risks);
    }
}
