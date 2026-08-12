using TourKit.Application.Common;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;
using TourKit.Application.Sales.Validators;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.Sales;

/// <summary>
/// Cơ hội bán hàng — bám <c>BookingTicket</c> của hệ cũ.
///
/// Trọng tâm là hai luật mà hệ cũ phải dựng riêng hộp thoại để chặn, và một luật ta thêm vào sau khi
/// đọc cách hệ cũ chốt đơn:
///  - huỷ BẮT BUỘC có lý do (nguồn của báo cáo thống kê lý do huỷ);
///  - "Chốt đơn" KHÔNG đặt tay — chỉ luồng đặt chỗ mới được đánh dấu;
///  - cơ hội đã chốt thì khoá, vì số liệu của nó đã đi vào đơn hàng.
/// </summary>
public class SalesOpportunityServiceTests
{
    private sealed class FakeCurrentUser : TourKit.Shared.Security.ICurrentUserContext
    {
        public Guid? UserId => Guid.Empty;
    }

    private static SalesOpportunityService NewService(
        out FakeRepository<SalesOpportunity> repo,
        out FakeRepository<SalesOpportunityAssignee> assignees,
        out FakeRepository<OpportunityStage> stages,
        out FakeRepository<TransferReason> reasons)
    {
        repo = new FakeRepository<SalesOpportunity>();
        assignees = new FakeRepository<SalesOpportunityAssignee>();
        stages = new FakeRepository<OpportunityStage>();
        reasons = new FakeRepository<TransferReason>();

        foreach (var (code, name, sys) in OpportunityStageCode.MacDinh)
        {
            stages.AddAsync(new OpportunityStage { Code = code, Name = name, IsSystem = sys, SortOrder = code }).Wait();
        }
        stages.SaveChangesAsync().Wait();

        return new SalesOpportunityService(repo, assignees, stages, reasons,
            new CreateSalesOpportunityValidator(), new UpdateSalesOpportunityValidator(), new FakeCurrentUser());
    }

    private static CreateSalesOpportunityDto NewDto(string ma = "CH-01") => new(
        Code: ma, Title: "Chị Lan hỏi Đà Nẵng 4N3Đ", Content: null,
        ContactName: "Nguyễn Thị Lan", ContactPhone: "0900000000", ContactEmail: null, ContactAddress: null,
        CustomerId: null,
        AdultQty: 2, ChildQty: 1, ChildSmallQty: 0, BabyQty: 0,
        PriceAdult: 5_000_000m, PriceChild: 3_000_000m, PriceChildSmall: 0m, PriceBaby: 0m,
        TemplateId: null, TourDepartureId: null);

    // ---- Giá trị phễu: lý do màn này tồn tại ----

    [Fact]
    public async Task Gia_tri_du_kien_cong_du_bon_bac_khach()
    {
        var svc = NewService(out _, out _, out _, out _);

        var dto = await svc.CreateAsync(NewDto() with
        {
            AdultQty = 2, ChildQty = 1, ChildSmallQty = 3, BabyQty = 4,
            PriceAdult = 10m, PriceChild = 100m, PriceChildSmall = 1_000m, PriceBaby = 10_000m,
        });

        // 2×10 + 1×100 + 3×1000 + 4×10000 = 43.120. Bậc trẻ nhỏ là bậc hệ cũ KHÔNG có ở phiếu —
        // bỏ nó đi thì con số này thiếu 3.000 mà không có gì báo.
        Assert.Equal(43_120m, dto.EstimatedValue);
    }

    // ---- Luật 1: huỷ phải có lý do ----

    [Fact]
    public async Task Huy_ma_khong_chon_ly_do_thi_tu_choi()
    {
        var svc = NewService(out _, out _, out _, out _);
        var ch = await svc.CreateAsync(NewDto());

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(OpportunityStageCode.Huy)));
    }

    [Fact]
    public async Task Huy_voi_ly_do_khong_co_that_thi_tu_choi()
    {
        var svc = NewService(out _, out _, out _, out _);
        var ch = await svc.CreateAsync(NewDto());

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(OpportunityStageCode.Huy, Guid.NewGuid())));
    }

    [Fact]
    public async Task Huy_kem_ly_do_hop_le_thi_ghi_lai_ly_do()
    {
        var svc = NewService(out _, out _, out _, out var reasons);
        var lyDo = new TransferReason { Name = "Khách chốt bên khác" };
        await reasons.AddAsync(lyDo);
        await reasons.SaveChangesAsync();

        var ch = await svc.CreateAsync(NewDto());
        var sau = await svc.MoveStageAsync(ch.Id,
            new MoveOpportunityStageDto(OpportunityStageCode.Huy, lyDo.Id, "Giá cao hơn 10%"));

        Assert.Equal(OpportunityStageCode.Huy, sau.StageCode);
        Assert.Equal(lyDo.Id, sau.CancelReasonId);
        Assert.Equal("Giá cao hơn 10%", sau.CancelNote);
    }

    [Fact]
    public async Task Chuyen_ra_khoi_Huy_thi_xoa_ly_do_cu()
    {
        // Lý do treo lại trên một cơ hội đang chạy sẽ chui vào báo cáo lý do huỷ và thổi phồng nó.
        var svc = NewService(out _, out _, out _, out var reasons);
        var lyDo = new TransferReason { Name = "Hết chỗ" };
        await reasons.AddAsync(lyDo);
        await reasons.SaveChangesAsync();

        var ch = await svc.CreateAsync(NewDto());
        await svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(OpportunityStageCode.Huy, lyDo.Id));
        var sau = await svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(OpportunityStageCode.DangXuLy));

        Assert.Null(sau.CancelReasonId);
        Assert.Null(sau.CancelNote);
    }

    // ---- Luật 2: chốt đơn không đặt tay ----

    [Fact]
    public async Task Khong_tu_keo_sang_Chot_don_duoc()
    {
        // Hệ cũ đánh dấu cột này từ trong luồng đặt khách lên tour, NGAY SAU khi đơn thật đã tạo.
        // Cho kéo thẻ trên kanban là sinh ra cơ hội đã chốt mà không có đơn nào phía sau.
        var svc = NewService(out _, out _, out _, out _);
        var ch = await svc.CreateAsync(NewDto());

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(OpportunityStageCode.ChotDon)));
    }

    [Fact]
    public async Task Co_hoi_da_chot_thi_khoa_sua_xoa_va_chuyen_cot()
    {
        var svc = NewService(out var repo, out _, out _, out _);
        var ch = await svc.CreateAsync(NewDto());

        // Giả lập đã chốt: luồng đặt chỗ gắn đơn vào cơ hội.
        var e = await repo.GetByIdAsync(ch.Id);
        e!.ConvertedOrderId = Guid.NewGuid();
        e.StageCode = OpportunityStageCode.ChotDon;
        repo.Update(e);
        await repo.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(OpportunityStageCode.DangXuLy)));
        await Assert.ThrowsAsync<ConflictException>(() => svc.DeleteAsync(ch.Id));
    }

    [Fact]
    public async Task Chuyen_sang_cot_khong_ton_tai_thi_tu_choi()
    {
        var svc = NewService(out _, out _, out _, out _);
        var ch = await svc.CreateAsync(NewDto());

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.MoveStageAsync(ch.Id, new MoveOpportunityStageDto(99)));
    }

    // ---- Mã cơ hội ----

    [Fact]
    public async Task Trung_ma_thi_tu_choi_voi_thong_bao_ro()
    {
        var svc = NewService(out _, out _, out _, out _);
        await svc.CreateAsync(NewDto("CH-TRUNG"));

        var loi = await Assert.ThrowsAsync<ConflictException>(() => svc.CreateAsync(NewDto("CH-TRUNG")));
        Assert.Contains("CH-TRUNG", loi.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sua_chinh_no_ma_giu_nguyen_ma_thi_khong_bao_trung()
    {
        var svc = NewService(out _, out _, out _, out _);
        var ch = await svc.CreateAsync(NewDto("CH-02"));

        var sua = await svc.UpdateAsync(ch.Id, new UpdateSalesOpportunityDto(
            Code: "CH-02", Title: "Đổi tên", Content: null,
            ContactName: "Nguyễn Thị Lan", ContactPhone: null, ContactEmail: null, ContactAddress: null,
            CustomerId: null,
            AdultQty: 2, ChildQty: 0, ChildSmallQty: 0, BabyQty: 0,
            PriceAdult: 1m, PriceChild: 0m, PriceChildSmall: 0m, PriceBaby: 0m,
            TemplateId: null, TourDepartureId: null));

        Assert.Equal("Đổi tên", sua.Title);
    }

    // ---- Người phụ trách: chỗ hệ cũ nhét id vào cột chuỗi ----

    [Fact]
    public async Task Loc_theo_nguoi_phu_trach_ra_dung_co_hoi_cua_ho()
    {
        var svc = NewService(out _, out _, out _, out _);
        var toi = Guid.NewGuid();
        var nguoiKhac = Guid.NewGuid();

        await svc.CreateAsync(NewDto("CH-A") with { Assignees = [new OpportunityAssigneeDto(toi, false)] });
        await svc.CreateAsync(NewDto("CH-B") with { Assignees = [new OpportunityAssigneeDto(nguoiKhac, false)] });

        var cuaToi = await svc.ListAsync(1, 20, new SalesOpportunityListFilter(AssigneeUserId: toi));

        Assert.Equal("CH-A", Assert.Single(cuaToi.Items).Code);
    }

    [Fact]
    public async Task Gan_trung_mot_nguoi_hai_lan_khong_de_ra_dong_thua()
    {
        // Dòng thừa làm mọi phép đếm "mỗi người giữ bao nhiêu cơ hội" sai lệch.
        var svc = NewService(out _, out var assignees, out _, out _);
        var uid = Guid.NewGuid();

        var ch = await svc.CreateAsync(NewDto() with
        {
            Assignees = [new OpportunityAssigneeDto(uid, false), new OpportunityAssigneeDto(uid, false)],
        });

        Assert.Single(await assignees.ListAsync(a => a.OpportunityId == ch.Id));
    }

    [Fact]
    public async Task Cung_mot_nguoi_vua_phu_trach_vua_theo_doi_thi_giu_ca_hai_vai()
    {
        var svc = NewService(out _, out var assignees, out _, out _);
        var uid = Guid.NewGuid();

        var ch = await svc.CreateAsync(NewDto() with
        {
            Assignees = [new OpportunityAssigneeDto(uid, false), new OpportunityAssigneeDto(uid, true)],
        });

        Assert.Equal(2, (await assignees.ListAsync(a => a.OpportunityId == ch.Id)).Count);
    }

    [Fact]
    public async Task Sua_voi_danh_sach_rong_thi_go_het_nguoi_phu_trach()
    {
        var svc = NewService(out _, out var assignees, out _, out _);
        var ch = await svc.CreateAsync(NewDto() with
        {
            Assignees = [new OpportunityAssigneeDto(Guid.NewGuid(), false)],
        });

        await svc.UpdateAsync(ch.Id, new UpdateSalesOpportunityDto(
            Code: ch.Code, Title: ch.Title, Content: null,
            ContactName: ch.ContactName, ContactPhone: null, ContactEmail: null, ContactAddress: null,
            CustomerId: null,
            AdultQty: 1, ChildQty: 0, ChildSmallQty: 0, BabyQty: 0,
            PriceAdult: 0m, PriceChild: 0m, PriceChildSmall: 0m, PriceBaby: 0m,
            TemplateId: null, TourDepartureId: null,
            Assignees: []));

        Assert.Empty(await assignees.ListAsync(a => a.OpportunityId == ch.Id));
    }

    // ---- Thẻ thống kê ----

    [Fact]
    public async Task Thong_ke_tach_gia_tri_dang_mo_khoi_tong()
    {
        // Tổng gộp cả cơ hội đã huỷ thì luôn đẹp và luôn vô nghĩa — người bán cần biết còn bao nhiêu
        // tiền ĐANG chạy.
        var svc = NewService(out _, out _, out _, out var reasons);
        var lyDo = new TransferReason { Name = "Khách đổi ý" };
        await reasons.AddAsync(lyDo);
        await reasons.SaveChangesAsync();

        await svc.CreateAsync(NewDto("CH-1") with
        {
            AdultQty = 1, ChildQty = 0, ChildSmallQty = 0, BabyQty = 0, PriceAdult = 100m,
        });
        var huy = await svc.CreateAsync(NewDto("CH-2") with
        {
            AdultQty = 1, ChildQty = 0, ChildSmallQty = 0, BabyQty = 0, PriceAdult = 900m,
        });
        await svc.MoveStageAsync(huy.Id, new MoveOpportunityStageDto(OpportunityStageCode.Huy, lyDo.Id));

        var tk = await svc.GetStatsAsync();

        Assert.Equal(2, tk.Total);
        Assert.Equal(1_000m, tk.TongGiaTri);
        Assert.Equal(100m, tk.GiaTriDangMo);
        Assert.Equal(1, tk.DaHuy);
        Assert.Equal(0, tk.DaChot);
    }

    [Fact]
    public async Task Thong_ke_dem_theo_tung_cot_cau_hinh_duoc()
    {
        var svc = NewService(out _, out _, out _, out _);
        await svc.CreateAsync(NewDto("CH-1"));
        var b = await svc.CreateAsync(NewDto("CH-2"));
        await svc.MoveStageAsync(b.Id, new MoveOpportunityStageDto(OpportunityStageCode.DangXuLy));

        var tk = await svc.GetStatsAsync();

        Assert.Equal(1, tk.TheoCot[OpportunityStageCode.TaoMoi]);
        Assert.Equal(1, tk.TheoCot[OpportunityStageCode.DangXuLy]);
    }

    // ---- Xác thực đầu vào ----

    [Fact]
    public async Task So_khach_am_thi_tu_choi()
    {
        // Một dòng âm làm tổng trên thẻ thống kê tụt xuống mà không có gì giải thích được.
        var svc = NewService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(NewDto() with { AdultQty = -1 }));
    }

    [Fact]
    public async Task Thieu_ma_hoac_ten_khach_thi_tu_choi()
    {
        var svc = NewService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => svc.CreateAsync(NewDto() with { Code = "" }));
        await Assert.ThrowsAsync<ValidationAppException>(() => svc.CreateAsync(NewDto() with { ContactName = "" }));
    }

    [Fact]
    public async Task Loc_tu_khoa_tim_theo_ma_ten_khach_va_sdt()
    {
        var svc = NewService(out _, out _, out _, out _);
        await svc.CreateAsync(NewDto("CH-DN-01") with { Title = "Đà Nẵng 4N3Đ", ContactName = "Chị Lan", ContactPhone = "0911111111" });
        await svc.CreateAsync(NewDto("CH-PQ-02") with { Title = "Phú Quốc 3N2Đ", ContactName = "Anh Bình", ContactPhone = "0922222222" });

        Assert.Single((await svc.ListAsync(1, 20, new SalesOpportunityListFilter(Q: "CH-DN"))).Items);
        Assert.Single((await svc.ListAsync(1, 20, new SalesOpportunityListFilter(Q: "Phú Quốc"))).Items);
        Assert.Single((await svc.ListAsync(1, 20, new SalesOpportunityListFilter(Q: "0922"))).Items);

        // Chuỗi vô nghĩa phải ra RỖNG — đây đúng là bài mà bộ e2e canh trên màn thật.
        Assert.Empty((await svc.ListAsync(1, 20, new SalesOpportunityListFilter(Q: "zzqqxx-khong-ton-tai"))).Items);
    }

    [Fact]
    public async Task Cot_phe_mac_dinh_sap_theo_thu_tu_va_danh_dau_cot_he_thong()
    {
        var svc = NewService(out _, out _, out _, out _);

        var cot = await svc.ListStagesAsync();

        Assert.Equal(6, cot.Count);
        Assert.Equal(OpportunityStageCode.TaoMoi, cot[0].Code);
        // Huỷ và Chốt đơn là cột hệ thống: luật bám vào mã của chúng nên không cho đổi/xoá.
        Assert.True(cot.Single(c => c.Code == OpportunityStageCode.Huy).IsSystem);
        Assert.True(cot.Single(c => c.Code == OpportunityStageCode.ChotDon).IsSystem);
    }
}
