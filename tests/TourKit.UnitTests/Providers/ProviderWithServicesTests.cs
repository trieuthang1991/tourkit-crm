using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using ProviderCrudService = TourKit.Application.Providers.ProviderService;
using ProviderServiceEntity = TourKit.Shared.Entities.ProviderService;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Sửa nhà cung cấp CÙNG bảng dịch vụ trong một lần ghi — bám hệ cũ.
///
/// Bản mới tách đôi thành hai màn, nhưng hệ cũ (KojiCRM) để chung: EditHotel.aspx có panel
/// "SẢN PHẨM/DỊCH VỤ" với các dòng thêm động, và uspInsertHotel nhận cả danh sách rồi ghi chung một
/// transaction. Tách ra lưu từng dòng là bỏ mất ràng buộc nguyên tử mà hệ cũ cố ý có.
/// </summary>
public class ProviderWithServicesTests
{
    private static ProviderCrudService NewService(
        out FakeRepository<Provider> nccRepo,
        out FakeRepository<ProviderServiceEntity> dvRepo)
    {
        nccRepo = new FakeRepository<Provider>();
        dvRepo = new FakeRepository<ProviderServiceEntity>();
        return new ProviderCrudService(nccRepo, dvRepo, new FakeRepository<OrderCost>(),
            new FakeRepository<PaymentVoucher>(), new CreateProviderValidator(), new UpdateProviderValidator());
    }

    private static UpdateProviderDto Ncc(string ten) =>
        new(ten, ProviderType.Hotel, null, null, null, null, null, null, null, null, 4, 1);

    private static ProviderServiceLineDto Dong(Guid? id, string ten, decimal giaVon, decimal giaBan) =>
        new(id, null, ten, giaVon, giaBan, "VND", 2, null, 1);

    [Fact]
    public async Task Sua_NCC_va_them_dich_vu_trong_cung_mot_lan_ghi()
    {
        var svc = NewService(out var nccRepo, out var dvRepo);
        var ncc = new Provider { Code = "KS01", Name = "Mường Thanh", Type = ProviderType.Hotel };
        await nccRepo.AddAsync(ncc);
        await nccRepo.SaveChangesAsync();

        await svc.UpdateWithServicesAsync(ncc.Id, Ncc("Mường Thanh Hà Nội"),
        [
            Dong(null, "Phòng Deluxe", 900_000m, 1_200_000m),
            Dong(null, "Phòng Superior", 700_000m, 950_000m),
        ]);

        Assert.Equal("Mường Thanh Hà Nội", (await nccRepo.GetByIdAsync(ncc.Id))!.Name);

        var dv = await dvRepo.ListAsync(x => x.ProviderId == ncc.Id);
        Assert.Equal(2, dv.Count);
        Assert.Contains(dv, x => x.PriceName == "Phòng Deluxe" && x.ContractPrice == 900_000m);
    }

    /// <summary>
    /// Dòng có sẵn mà KHÔNG nằm trong danh sách gửi lên nghĩa là người dùng đã bỏ nó khỏi form —
    /// phải xoá. Giữ lại thì form hiển thị một đằng, dữ liệu một nẻo.
    /// </summary>
    [Fact]
    public async Task Dong_bi_bo_khoi_form_thi_bi_xoa()
    {
        var svc = NewService(out var nccRepo, out var dvRepo);
        var ncc = new Provider { Code = "KS02", Name = "Khách sạn B", Type = ProviderType.Hotel };
        await nccRepo.AddAsync(ncc);
        await nccRepo.SaveChangesAsync();

        var giu = new ProviderServiceEntity { ProviderId = ncc.Id, PriceName = "Giữ lại", ContractPrice = 100m };
        var bo = new ProviderServiceEntity { ProviderId = ncc.Id, PriceName = "Bỏ đi", ContractPrice = 200m };
        await dvRepo.AddAsync(giu);
        await dvRepo.AddAsync(bo);
        await dvRepo.SaveChangesAsync();

        // Gửi lên CHỈ dòng cần giữ.
        await svc.UpdateWithServicesAsync(ncc.Id, Ncc("Khách sạn B"),
            [Dong(giu.Id, "Giữ lại (đã sửa)", 150m, 200m)]);

        var conLai = await dvRepo.ListAsync(x => x.ProviderId == ncc.Id);
        Assert.Single(conLai);
        Assert.Equal("Giữ lại (đã sửa)", conLai[0].PriceName);
        Assert.Equal(150m, conLai[0].ContractPrice);
    }

    /// <summary>
    /// NGUYÊN TỬ: tên NCC rỗng là dữ liệu không hợp lệ — phải từ chối TRƯỚC khi động vào dịch vụ.
    /// Nếu không, người dùng nhận lỗi nhưng bảng giá đã bị sửa mất rồi.
    /// </summary>
    [Fact]
    public async Task NCC_khong_hop_le_thi_khong_dung_gi_toi_dich_vu()
    {
        var svc = NewService(out var nccRepo, out var dvRepo);
        var ncc = new Provider { Code = "KS03", Name = "Khách sạn C", Type = ProviderType.Hotel };
        await nccRepo.AddAsync(ncc);
        await nccRepo.SaveChangesAsync();

        var cu = new ProviderServiceEntity { ProviderId = ncc.Id, PriceName = "Nguyên trạng", ContractPrice = 500m };
        await dvRepo.AddAsync(cu);
        await dvRepo.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.UpdateWithServicesAsync(ncc.Id, Ncc(""), [Dong(null, "Dòng mới", 1m, 2m)]));

        var sau = await dvRepo.ListAsync(x => x.ProviderId == ncc.Id);
        Assert.Single(sau);
        Assert.Equal("Nguyên trạng", sau[0].PriceName);
        Assert.Equal("Khách sạn C", (await nccRepo.GetByIdAsync(ncc.Id))!.Name);
    }

    [Fact]
    public async Task NCC_khong_ton_tai_thi_bao_khong_tim_thay()
    {
        var svc = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateWithServicesAsync(Guid.NewGuid(), Ncc("X"), []));
    }
}
