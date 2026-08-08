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




    /// <summary>Khai thẳng mã thật vẫn chạy — không bắt ai phải đặt tên cho mọi model.</summary>


    /// <summary>Chưa khai bảng tên = không giới hạn — để dùng được model mới mà chưa kịp cập nhật.</summary>


    [Fact]
    public void Nha_cung_cap_gia_khong_can_dia_chi()
    {
        var options = new AiOptions { Enabled = true };
        var fake = new AiProviderOptions { Kind = "Log", BaseUrl = "", ApiKey = "", TimeoutSeconds = 5 };
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
            Assert.NotEqual(name, AiFeatures.Label(name));   // phải có nhãn tiếng Việt riêng
        }
    }
}
