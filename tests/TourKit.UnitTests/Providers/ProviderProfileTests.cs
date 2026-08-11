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

    // ================= Trường riêng ở mức DÒNG bảng giá =================

    private static ProviderCrudService NewServiceCoDichVu(
        out FakeRepository<Provider> nccRepo,
        out FakeRepository<ProviderServiceEntity> dvRepo)
    {
        nccRepo = new FakeRepository<Provider>();
        dvRepo = new FakeRepository<ProviderServiceEntity>();
        return new ProviderCrudService(nccRepo, dvRepo, new FakeRepository<OrderCost>(),
            new FakeRepository<PaymentVoucher>(), new CreateProviderValidator(), new UpdateProviderValidator());
    }

    private static ProviderServiceLineDto Dong(Guid? id, ProviderServiceLineProfile? hoSo) =>
        new(id, null, "Gói A", 100m, 150m, "VND", 2, null, 1, hoSo);

    [Fact]
    public void Doi_sang_loai_khac_thi_bo_truong_cua_loai_cu()
    {
        var khachSan = new ProviderServiceLineProfile
        {
            PeriodFrom = new DateOnly(2026, 1, 1),
            DayType = "Cuối tuần",
            NetCostPerDay = 500_000m,
        };

        var sangVe = khachSan.ChiGiuCuaLoai(ProviderType.Airline);

        Assert.Null(sangVe.PeriodFrom);
        Assert.Null(sangVe.DayType);
        Assert.Null(sangVe.NetCostPerDay);
        Assert.Null(sangVe.ToJsonOrNull());   // không còn gì thì đừng lưu JSON rỗng
    }

    [Fact]
    public async Task Luu_dong_gia_khach_san_roi_doc_lai_van_du_truong_rieng()
    {
        var svc = NewServiceCoDichVu(out var nccRepo, out var dvRepo);
        var ncc = new Provider { Code = "KS10", Name = "Mường Thanh", Type = ProviderType.Hotel };
        await nccRepo.AddAsync(ncc);
        await nccRepo.SaveChangesAsync();

        await svc.UpdateWithServicesAsync(ncc.Id,
            new UpdateProviderDto("Mường Thanh", ProviderType.Hotel, null, null, null, null, null, null, null, null, 4, 1),
            [Dong(null, new ProviderServiceLineProfile
            {
                PeriodFrom = new DateOnly(2026, 6, 1),
                PeriodTo = new DateOnly(2026, 8, 31),
                DayType = "Ngày lễ",
                NetCostPerDay = 1_200_000m,
            })]);

        var dong = Assert.Single(await dvRepo.ListAsync(s => s.ProviderId == ncc.Id));
        var hoSo = ProviderServiceLineProfile.Parse(dong.ProfileJson);
        Assert.Equal(new DateOnly(2026, 6, 1), hoSo.PeriodFrom);
        Assert.Equal("Ngày lễ", hoSo.DayType);
        Assert.Equal(1_200_000m, hoSo.NetCostPerDay);
    }

    /// <summary>
    /// Đổi loại NCC thì trường riêng của dòng phải mất theo, KỂ CẢ khi lần lưu đó không gửi lại hồ sơ
    /// dòng. Nếu không, một NCC vé máy bay vẫn ngầm giữ "loại ngày" của thời còn là khách sạn —
    /// giao diện không còn chỗ nào hiện ra để sửa hay xoá.
    /// </summary>
    [Fact]
    public async Task Doi_loai_NCC_thi_truong_rieng_cua_dong_mat_theo()
    {
        var svc = NewServiceCoDichVu(out var nccRepo, out var dvRepo);
        var ncc = new Provider { Code = "KS11", Name = "Sẽ đổi loại", Type = ProviderType.Hotel };
        await nccRepo.AddAsync(ncc);
        var dong = new ProviderServiceEntity
        {
            ProviderId = ncc.Id,
            ProfileJson = new ProviderServiceLineProfile { DayType = "Cuối tuần" }.ToJsonOrNull(),
        };
        await dvRepo.AddAsync(dong);
        await nccRepo.SaveChangesAsync();
        await dvRepo.SaveChangesAsync();

        // Lưu lại với loại MỚI, dòng không kèm hồ sơ (Profile null).
        await svc.UpdateWithServicesAsync(ncc.Id,
            new UpdateProviderDto("Đã thành hãng bay", ProviderType.Airline, null, null, null, null, null, null, null, null, 4, 1),
            [Dong(dong.Id, null)]);

        Assert.Null(ProviderServiceLineProfile.Parse(
            (await dvRepo.ListAsync(s => s.ProviderId == ncc.Id)).Single().ProfileJson).DayType);
    }

    [Theory]
    [InlineData(ProviderType.Hotel, true, false, false)]
    [InlineData(ProviderType.Vehicle, false, true, false)]
    [InlineData(ProviderType.Voucher, false, false, true)]
    [InlineData(ProviderType.Restaurant, false, false, false)]
    [InlineData(ProviderType.Airline, false, false, false)]
    public void Loai_nao_hien_truong_nay(ProviderType loai, bool ks, bool xe, bool vc)
    {
        Assert.Equal(ks, ProviderProfile.CoTruongKhachSan(loai));
        Assert.Equal(xe, ProviderProfile.CoTruongXe(loai));
        Assert.Equal(vc, ProviderProfile.CoTruongVoucher(loai));
    }

    /// <summary>
    /// Voucher là loại NCC thứ 7, KHÔNG lấy số 2 như <c>ServicesType.Vouchers</c> bên hệ cũ: số 2 ở
    /// enum này đã là Vehicle và đang có dữ liệu. Đánh lại số là đổi nghĩa mọi dòng NCC đã lưu.
    /// </summary>
    [Fact]
    public void Voucher_khong_duoc_lay_so_cua_loai_dang_co_du_lieu()
    {
        Assert.Equal(7, (int)ProviderType.Voucher);
        Assert.Equal(2, (int)ProviderType.Vehicle);
    }

    [Fact]
    public async Task Voucher_luu_class_hotel_va_ten_du_an_roi_doc_lai_van_con()
    {
        var svc = NewService(out _);

        var tao = await svc.CreateAsync(new CreateProviderDto(
            "VC01", "Gói nghỉ dưỡng", ProviderType.Voucher, null, null, null, null, null, null, null, null, 0, 1,
            Profile: new ProviderProfile { HotelClass = "4 sao", ProjectName = "Dự án Hạ Long" }));

        var doc = await svc.GetAsync(tao.Id);
        Assert.Equal("4 sao", doc.Profile!.HotelClass);
        Assert.Equal("Dự án Hạ Long", doc.Profile.ProjectName);
    }
}
