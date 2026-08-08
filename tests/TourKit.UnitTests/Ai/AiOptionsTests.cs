using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Soát cấu hình AI. Mọi lỗi ở đây đều KHÔNG gây ngoại lệ khi chạy — chúng chỉ làm tính năng im lặng
/// không hoạt động, nên bắt được lúc khởi động mới có giá trị.
/// </summary>
public class AiOptionsTests
{
    private static AiOptions WithDeepSeek(Action<AiFeatureOptions>? tweak = null, string feature = AiFeatures.Assistant)
    {
        var options = new AiOptions { Enabled = true };
        var provider = new AiProviderOptions
        {
            Kind = "OpenAiCompatible",
            BaseUrl = "https://api.deepseek.com/v1",
            ApiKey = "sk-test",
        };
        provider.Capabilities.Add("Chat");
        options.Providers["deepseek"] = provider;

        var settings = new AiFeatureOptions { Enabled = true, Provider = "deepseek", Model = "deepseek-chat" };
        tweak?.Invoke(settings);
        options.Features[feature] = settings;

        return options;
    }

    [Fact]
    public void Cau_hinh_dung_thi_khong_co_loi()
    {
        Assert.Empty(WithDeepSeek().Validate());
    }

    [Fact]
    public void Ten_tinh_nang_go_sai_bi_bat_ngay()
    {
        var options = WithDeepSeek(feature: "Assistan");   // thiếu chữ t

        var errors = options.Validate();

        Assert.Contains(errors, e => e.Contains("Assistan", StringComparison.Ordinal)
                                  && e.Contains("không phải tính năng đã biết", StringComparison.Ordinal));
    }

    [Fact]
    public void Tro_toi_nha_cung_cap_khong_ton_tai_bi_bat()
    {
        var options = WithDeepSeek(s => s.Provider = "deepsek");

        var errors = options.Validate();

        Assert.Contains(errors, e => e.Contains("deepsek", StringComparison.Ordinal)
                                  && e.Contains("không có trong Ai:Providers", StringComparison.Ordinal));
    }

    [Fact]
    public void Bo_trong_model_bi_bat_thay_vi_lay_mac_dinh_ngam()
    {
        var errors = WithDeepSeek(s => s.Model = "").Validate();

        Assert.Contains(errors, e => e.Contains("Model đang trống", StringComparison.Ordinal));
    }

    /// <summary>
    /// DeepSeek có hội thoại nhưng KHÔNG có API nhúng vector. Trỏ tính năng Embedding vào nó là một lỗi
    /// chỉ lộ ra khi làm tới RAG — bắt ngay lúc khởi động thay vì để dành.
    /// </summary>
    [Fact]
    public void Nha_cung_cap_thieu_nang_luc_can_thiet_bi_bat()
    {
        var options = WithDeepSeek(feature: AiFeatures.Embedding);

        var errors = options.Validate();

        Assert.Contains(errors, e => e.Contains("cần năng lực Embedding", StringComparison.Ordinal));
    }

    /// <summary>Tính năng tắt vì gõ sai trông y hệt tính năng tắt có chủ ý — vẫn phải soát.</summary>
    [Fact]
    public void Tinh_nang_dang_tat_van_bi_soat_neu_da_khai_bao()
    {
        var options = WithDeepSeek(s =>
        {
            s.Enabled = false;
            s.Provider = "deepsek";
        });

        Assert.Contains(options.Validate(), e => e.Contains("không có trong Ai:Providers", StringComparison.Ordinal));
    }

    /// <summary>
    /// Bật một tính năng chưa viết thì không có gì hỏng — và đó mới là vấn đề: người đọc cấu hình
    /// tưởng nó đang chạy, còn chốt chặn khởi động đòi khoá cho một thứ không tồn tại.
    /// </summary>
    [Fact]
    public void Bat_tinh_nang_chua_trien_khai_thi_bi_chan()
    {
        var options = WithDeepSeek(feature: AiFeatures.Draft);

        var errors = options.Validate();

        Assert.Contains(errors, e => e.Contains("CHƯA được triển khai", StringComparison.Ordinal));
    }

    [Fact]
    public void Tinh_nang_chua_trien_khai_nhung_dang_tat_thi_khong_sao()
    {
        var options = WithDeepSeek(s => s.Enabled = false, AiFeatures.Draft);

        Assert.Empty(options.Validate());
    }

    /// <summary>Danh sách đã triển khai phải là tập con của danh mục — chống gõ sai tên.</summary>
    [Fact]
    public void Danh_sach_da_trien_khai_nam_trong_danh_muc()
    {
        Assert.All(AiFeatures.Implemented, name => Assert.True(AiFeatures.IsKnown(name), name));
        Assert.Contains(AiFeatures.Assistant, AiFeatures.Implemented);
    }

    [Fact]
    public void Tinh_nang_chua_khai_bao_gi_thi_khong_bao_loi()
    {
        var options = WithDeepSeek();
        options.Features[AiFeatures.Embedding] = new AiFeatureOptions { Enabled = false };

        Assert.Empty(options.Validate());
    }

    [Fact]
    public void So_vong_goi_cong_cu_ngoai_khoang_bi_bat()
    {
        Assert.Contains(WithDeepSeek(s => s.MaxToolRounds = 0).Validate(),
            e => e.Contains("MaxToolRounds", StringComparison.Ordinal));
        Assert.Contains(WithDeepSeek(s => s.MaxToolRounds = 50).Validate(),
            e => e.Contains("MaxToolRounds", StringComparison.Ordinal));
    }

    [Fact]
    public void Dia_chi_khong_phai_http_bi_bat()
    {
        var options = WithDeepSeek();
        options.Providers["deepseek"].BaseUrl = "api.deepseek.com";

        Assert.Contains(options.Validate(), e => e.Contains("BaseUrl", StringComparison.Ordinal));
    }

    /// <summary>
    /// Hãng đã khai danh mục thì mã model phải nằm trong đó. Không soát thì mã sai chỉ lộ ra lúc
    /// người dùng bấm nút và nhận về một lỗi khó hiểu của hãng.
    /// </summary>
    [Fact]
    public void Model_ngoai_danh_muc_cua_hang_bi_bat()
    {
        var options = WithDeepSeek(s => s.Model = "deepseek-chatt");
        options.Providers["deepseek"].Models.Add("deepseek-chat");

        var errors = options.Validate();

        Assert.Contains(errors, e => e.Contains("deepseek-chatt", StringComparison.Ordinal)
                                  && e.Contains("không có trong danh mục model", StringComparison.Ordinal));
    }

    [Fact]
    public void Model_trong_danh_muc_thi_hop_le()
    {
        var options = WithDeepSeek();
        options.Providers["deepseek"].Models.Add("deepseek-chat");

        Assert.Empty(options.Validate());
    }

    /// <summary>Chưa khai danh mục = không giới hạn — để dùng được model mới mà chưa kịp cập nhật.</summary>
    [Fact]
    public void Hang_chua_khai_danh_muc_thi_go_ma_nao_cung_duoc()
    {
        var options = WithDeepSeek(s => s.Model = "model-vua-ra-mat");

        Assert.Empty(options.Validate());
    }

    [Fact]
    public void Nang_luc_go_sai_bi_bat()
    {
        var options = WithDeepSeek();
        options.Providers["deepseek"].Capabilities.Add("Chatt");

        Assert.Contains(options.Validate(), e => e.Contains("Chatt", StringComparison.Ordinal));
    }

    [Fact]
    public void Nha_cung_cap_gia_khong_can_dia_chi()
    {
        var options = new AiOptions { Enabled = true };
        var fake = new AiProviderOptions { Kind = "Log", BaseUrl = "", ApiKey = "", TimeoutSeconds = 5 };
        fake.Capabilities.Add("Chat");
        options.Providers["log"] = fake;
        options.Features[AiFeatures.Assistant] =
            new AiFeatureOptions { Enabled = true, Provider = "log", Model = "gia-lap" };

        Assert.Empty(options.Validate());
        Assert.Empty(options.ProvidersMissingApiKey());
    }

    [Fact]
    public void Thieu_khoa_chi_tinh_cho_tinh_nang_dang_bat()
    {
        var options = WithDeepSeek();
        options.Providers["deepseek"].ApiKey = "";

        Assert.Equal(["deepseek"], options.ProvidersMissingApiKey());

        options.Features[AiFeatures.Assistant].Enabled = false;
        Assert.Empty(options.ProvidersMissingApiKey());
    }

    [Fact]
    public void Resolve_tra_ve_dung_nha_cung_cap_va_model()
    {
        var resolved = WithDeepSeek().Resolve(AiFeatures.Assistant);

        Assert.NotNull(resolved);
        Assert.Equal("deepseek", resolved.ProviderName);
        Assert.Equal("deepseek-chat", resolved.Settings.Model);
        Assert.Equal(60, resolved.TimeoutSeconds);   // tính năng không khai riêng → theo nhà cung cấp
    }

    [Fact]
    public void Resolve_uu_tien_thoi_gian_cho_khai_rieng_cua_tinh_nang()
    {
        var resolved = WithDeepSeek(s => s.TimeoutSeconds = 120).Resolve(AiFeatures.Assistant);

        Assert.Equal(120, resolved!.TimeoutSeconds);
    }

    /// <summary>Công tắc tổng tắt thì mọi tính năng ngừng, không cần sửa từng dòng cấu hình.</summary>
    [Fact]
    public void Cong_tac_tong_tat_thi_Resolve_tra_null()
    {
        var options = WithDeepSeek();
        options.Enabled = false;

        Assert.Null(options.Resolve(AiFeatures.Assistant));
    }

    [Fact]
    public void Resolve_tra_null_khi_tinh_nang_tat_hoac_chua_khai_bao()
    {
        var options = WithDeepSeek(s => s.Enabled = false);

        Assert.Null(options.Resolve(AiFeatures.Assistant));
        Assert.Null(options.Resolve(AiFeatures.Draft));
    }

    [Fact]
    public void Moi_ten_tinh_nang_trong_All_deu_tra_duoc_nang_luc_va_nhan()
    {
        foreach (var name in AiFeatures.All)
        {
            Assert.True(AiFeatures.IsKnown(name), name);
            Assert.NotNull(AiFeatures.Requires(name));
            Assert.NotEqual(name, AiFeatures.Label(name));   // phải có nhãn tiếng Việt riêng
        }
    }
}
