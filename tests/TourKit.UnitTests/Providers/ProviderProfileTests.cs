using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using ProviderCrudService = TourKit.Application.Providers.ProviderService;
using ProviderServiceEntity = TourKit.Shared.Entities.ProviderService;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Trường mềm riêng theo loại NCC, lưu gộp trong <c>Provider.ProfileJson</c>.
///
/// Hệ cũ (KojiCRM) có 4 màn sửa riêng dùng chung phần đầu rồi mỗi màn thêm ô của riêng nó:
/// EditHotel có "Năm xây dựng"/"Quốc gia", Edit (xe) có "Thông tin xe"/"Loại xe" chọn nhiều.
/// </summary>
public class ProviderProfileTests
{
    private static ProviderCrudService NewService(out FakeRepository<Provider> repo)
    {
        repo = new FakeRepository<Provider>();
        return new ProviderCrudService(repo, new FakeRepository<ProviderServiceEntity>(),
            new FakeRepository<OrderCost>(), new FakeRepository<PaymentVoucher>(),
            new CreateProviderValidator(), new UpdateProviderValidator());
    }

    [Fact]
    public void Rong_thi_khong_luu_JSON_thua()
    {
        Assert.Null(new ProviderProfile().ToJsonOrNull());
    }

    [Fact]
    public void Ghi_roi_doc_lai_giu_nguyen_ca_danh_sach_nhieu_gia_tri()
    {
        var goc = new ProviderProfile
        {
            BuiltYear = 2015,
            Country = "Việt Nam",
            VehicleTypes = ["4 chỗ", "16 chỗ"],
        };

        var lai = ProviderProfile.Parse(goc.ToJsonOrNull());

        Assert.Equal(2015, lai.BuiltYear);
        Assert.Equal("Việt Nam", lai.Country);
        Assert.Equal(["4 chỗ", "16 chỗ"], lai.VehicleTypes);
    }

    /// <summary>JSON hỏng không được làm sập màn danh sách — dữ liệu di trú có thể lỗi định dạng.</summary>
    [Fact]
    public void JSON_hong_thi_tra_ho_so_rong_chu_khong_nem_loi()
    {
        Assert.Null(ProviderProfile.Parse("{ khong-phai-json").BuiltYear);
    }

    [Fact]
    public async Task Tao_moi_co_ho_so_thi_ghi_xuong_va_doc_lai_duoc()
    {
        var svc = NewService(out _);

        var tao = await svc.CreateAsync(new CreateProviderDto(
            "KS01", "Mường Thanh", ProviderType.Hotel, null, null, null, null, null, null, null, null, 4, 1,
            Profile: new ProviderProfile { BuiltYear = 2010, Country = "Việt Nam" }));

        var doc = await svc.GetAsync(tao.Id);
        Assert.Equal(2010, doc.Profile!.BuiltYear);
        Assert.Equal("Việt Nam", doc.Profile.Country);
    }

    /// <summary>
    /// Đường sửa KHÔNG gửi Profile (API cũ, import) phải giữ nguyên hồ sơ đang có.
    ///
    /// Gán đè null ở đây là mọi lần sửa thiếu Profile sẽ âm thầm xoá sạch năm xây dựng/loại xe —
    /// đúng kiểu mất dữ liệu đã xảy ra ở form sửa chuyến đi.
    /// </summary>
    [Fact]
    public async Task Sua_ma_khong_gui_ho_so_thi_khong_xoa_ho_so_cu()
    {
        var svc = NewService(out var repo);
        var ncc = new Provider
        {
            Code = "KS02",
            Name = "Rex",
            Type = ProviderType.Hotel,
            ProfileJson = new ProviderProfile { BuiltYear = 1927 }.ToJsonOrNull(),
        };
        await repo.AddAsync(ncc);
        await repo.SaveChangesAsync();

        await svc.UpdateAsync(ncc.Id, new UpdateProviderDto(
            "Rex Saigon", ProviderType.Hotel, null, null, null, null, null, null, null, null, 5, 1));

        var doc = await svc.GetAsync(ncc.Id);
        Assert.Equal("Rex Saigon", doc.Name);
        Assert.Equal(1927, doc.Profile!.BuiltYear);
    }

    [Fact]
    public async Task Gui_ho_so_rong_thi_xoa_duoc_gia_tri_cu()
    {
        var svc = NewService(out var repo);
        var ncc = new Provider
        {
            Code = "KS03",
            Name = "Metropole",
            Type = ProviderType.Hotel,
            ProfileJson = new ProviderProfile { BuiltYear = 1901 }.ToJsonOrNull(),
        };
        await repo.AddAsync(ncc);
        await repo.SaveChangesAsync();

        await svc.UpdateAsync(ncc.Id, new UpdateProviderDto(
            "Metropole", ProviderType.Hotel, null, null, null, null, null, null, null, null, 5, 1,
            Profile: new ProviderProfile()));

        Assert.Null((await svc.GetAsync(ncc.Id)).Profile!.BuiltYear);
    }

    [Theory]
    [InlineData(ProviderType.Hotel, true, false)]
    [InlineData(ProviderType.Vehicle, false, true)]
    [InlineData(ProviderType.Restaurant, false, false)]
    [InlineData(ProviderType.Airline, false, false)]
    public void Loai_nao_hien_truong_nay(ProviderType loai, bool ks, bool xe)
    {
        Assert.Equal(ks, ProviderProfile.CoTruongKhachSan(loai));
        Assert.Equal(xe, ProviderProfile.CoTruongXe(loai));
    }
}
