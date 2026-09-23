using TourKit.Application.Customers;
using TourKit.Application.Customers.Validators;
using TourKit.Application.Common;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Crm.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test <see cref="LeadService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh, KHÔNG EF,
/// KHÔNG HTTP (cùng tinh thần với <c>ProviderServiceTests</c>).
/// </summary>
public class LeadServiceTests
{
    /// <summary>
    /// Dựng LeadService với một <see cref="CustomerService"/> THẬT chứ không phải bản giả.
    ///
    /// Có chủ ý: điều cần chứng minh ở đây là chuyển đổi đi ĐÚNG cửa tạo khách hàng — nơi sinh mã
    /// KH_, dựng cột tìm kiếm và chặn trùng số điện thoại. Thay bằng bản giả thì test vẫn xanh kể
    /// cả khi chuyển đổi lại lén ghi thẳng xuống kho như trước.
    /// </summary>
    private static LeadService NewService(out FakeRepository<Lead> repo, out FakeRepository<Customer> customerRepo)
    {
        repo = new FakeRepository<Lead>();
        customerRepo = new FakeRepository<Customer>();
        var customers = new CustomerService(
            customerRepo, new FakeRepository<Order>(), new FakeRepository<CustomerCare>(), new FakeRepository<User>(),
            new FakeCurrentUser(), new FakeCustomerQueries(),
            new CreateCustomerValidator(), new UpdateCustomerValidator());

        var campaigns = new LeadCampaignService(
            new FakeRepository<LeadCampaign>(), repo, new FakeRepository<User>());

        return new LeadService(
            repo, customers, campaigns,
            new CreateLeadValidator(), new UpdateLeadValidator(), new FakeCurrentUser());
    }

    /// <summary>
    /// Dựng LeadService kèm một chiến dịch CÓ THẬT để thử đường chia số.
    /// Trả ra kho chiến dịch để test tự gieo dữ liệu.
    /// </summary>
    private static LeadService NewServiceCoChienDich(
        out FakeRepository<Lead> repo, out FakeRepository<LeadCampaign> campaignRepo)
    {
        repo = new FakeRepository<Lead>();
        campaignRepo = new FakeRepository<LeadCampaign>();
        var customerRepo = new FakeRepository<Customer>();
        var customers = new CustomerService(
            customerRepo, new FakeRepository<Order>(), new FakeRepository<CustomerCare>(), new FakeRepository<User>(),
            new FakeCurrentUser(), new FakeCustomerQueries(),
            new CreateCustomerValidator(), new UpdateCustomerValidator());
        var campaigns = new LeadCampaignService(campaignRepo, repo, new FakeRepository<User>());

        return new LeadService(
            repo, customers, campaigns,
            new CreateLeadValidator(), new UpdateLeadValidator(), new FakeCurrentUser());
    }

    private sealed class FakeCurrentUser : TourKit.Shared.Security.ICurrentUserContext
    {
        public Guid? UserId => null;
    }

    private sealed class FakeCustomerQueries : ICustomerQueries
    {
        public Task<(int FirstTime, int Repeat)> BuyerCountsAsync() => Task.FromResult((0, 0));
    }

    private static CreateLeadDto NewCreateDto(string name = "Nguyen Van A") =>
        new(name, "0900000000", $"{name}@x.com", "facebook", null);

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo, out _);

        var dto = await service.CreateAsync(NewCreateDto());

        Assert.Equal("Nguyen Van A", dto.FullName);
        Assert.Equal(LeadStatus.New, dto.Status);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("Nguyen Van A", stored!.FullName);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_full_name_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateLeadDto("", "0900000000", null, null, null)));
    }

    [Fact]
    public async Task ConvertAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ConvertAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ConvertAsync_creates_customer_and_sets_converted_customer_id()
    {
        var service = NewService(out var repo, out var customerRepo);
        var created = await service.CreateAsync(NewCreateDto("Tran Thi B"));

        var result = await service.ConvertAsync(created.Id);

        var customer = await customerRepo.GetByIdAsync(result.CustomerId);
        Assert.NotNull(customer);
        Assert.Equal("Tran Thi B", customer!.FullName);

        var lead = await repo.GetByIdAsync(created.Id);
        Assert.Equal(LeadStatus.Won, lead!.Status);
        Assert.Equal(result.CustomerId, lead.ConvertedCustomerId);
    }

    /// <summary>
    /// Khách sinh ra từ chuyển đổi phải ĐẦY ĐỦ như khách tạo tay.
    ///
    /// Trước bản vá, <c>ConvertAsync</c> ghi thẳng vào kho nên bỏ qua cả ba thứ nằm trong
    /// CustomerService: mã KH_, <c>SearchName</c> (tìm không dấu) và <c>PhoneNormalized</c>.
    /// Hệ quả dây chuyền: khách đó TÌM KHÔNG RA ở màn Data khách hàng, và cũng không bao giờ lọt
    /// vào màn Rà khách trùng vì màn đó gom theo <c>PhoneNormalized</c>.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_khach_sinh_ra_co_du_ma_va_cot_tim_kiem()
    {
        var service = NewService(out _, out var customerRepo);
        var created = await service.CreateAsync(NewCreateDto("Đặng Thuỳ Dương"));

        var result = await service.ConvertAsync(created.Id);

        var customer = await customerRepo.GetByIdAsync(result.CustomerId);
        Assert.NotNull(customer);
        Assert.StartsWith("KH_", customer!.Code, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(customer.SearchName));
        Assert.False(string.IsNullOrWhiteSpace(customer.PhoneNormalized));
        Assert.Contains("duong", customer.SearchName!, StringComparison.Ordinal);   // đã bỏ dấu
    }

    /// <summary>
    /// Số điện thoại đã thuộc về một khách sẵn có thì GẮN vào khách đó, không tạo bản sao.
    ///
    /// Đây là điểm phải quyết: đi qua CustomerService nghĩa là luật chặn trùng số điện thoại bắt
    /// đầu có hiệu lực ở đường chuyển đổi. Ném lỗi thì người bán bị chặn giữa chừng mà không có
    /// lối đi tiếp; tạo thêm một hồ sơ nữa thì chính là thứ luật kia sinh ra để ngăn. Lead đó CHÍNH
    /// LÀ người đã có hồ sơ, nên nối vào là đúng nghiệp vụ.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_sdt_trung_thi_gan_vao_khach_san_co()
    {
        var service = NewService(out var repo, out var customerRepo);
        var sanCo = new Lead { FullName = "Người Đã Là Khách", Phone = "0900000000" };
        await repo.AddAsync(sanCo);
        await repo.SaveChangesAsync();
        var daLaKhach = await service.ConvertAsync(sanCo.Id);

        var leadMoi = await service.CreateAsync(NewCreateDto("Trùng Số"));
        var ketQua = await service.ConvertAsync(leadMoi.Id);

        Assert.Equal(daLaKhach.CustomerId, ketQua.CustomerId);       // cùng một hồ sơ khách
        Assert.True(ketQua.DaGanVaoKhachSanCo);
        Assert.Single(await customerRepo.ListAsync());                // KHÔNG sinh bản sao
        Assert.False(daLaKhach.DaGanVaoKhachSanCo);                   // lần đầu là tạo mới
    }

    /// <summary>
    /// Form thu lead gửi MÃ chiến dịch → lead tự gắn chiến dịch và tự có người phụ trách.
    /// Đây là toàn bộ lý do màn "Chia số Sale" tồn tại; trước đây không có đường nào gán CampaignId
    /// nên mọi con số của màn đó luôn bằng 0.
    /// </summary>
    [Fact]
    public async Task Tao_lead_theo_ma_chien_dich_thi_tu_chia_so_xoay_vong()
    {
        var service = NewServiceCoChienDich(out _, out var campaignRepo);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var cd = new LeadCampaign
        {
            Name = "Thu Đông",
            Code = "CD-2026-009",
            AssignMode = (int)LeadAssignMode.XoayVong,
            AssigneesJson = NhomChiaSo.ToJsonOrNull([a, b]),
        };
        await campaignRepo.AddAsync(cd);
        await campaignRepo.SaveChangesAsync();

        var l1 = await service.CreateAsync(NewCreateDto("Khach 1") with { CampaignCode = "CD-2026-009" });
        var l2 = await service.CreateAsync(NewCreateDto("Khach 2") with { CampaignCode = "CD-2026-009" });
        var l3 = await service.CreateAsync(NewCreateDto("Khach 3") with { CampaignCode = "CD-2026-009" });

        Assert.Equal(a, l1.AssignedToUserId);
        Assert.Equal(b, l2.AssignedToUserId);
        Assert.Equal(a, l3.AssignedToUserId);   // quay lại đầu vòng
    }

    /// <summary>
    /// Lời gọi đã CHỈ ĐỊNH người thì chia tự động không được đè lên: chia số là để lấp chỗ trống,
    /// không phải để lật quyết định của người dùng.
    /// </summary>
    [Fact]
    public async Task Da_chi_dinh_nguoi_thi_chia_so_khong_de_len()
    {
        var service = NewServiceCoChienDich(out _, out var campaignRepo);
        var trongNhom = Guid.NewGuid();
        var chiDinh = Guid.NewGuid();
        await campaignRepo.AddAsync(new LeadCampaign
        {
            Name = "X", Code = "CD-2026-010",
            AssignMode = (int)LeadAssignMode.XoayVong,
            AssigneesJson = NhomChiaSo.ToJsonOrNull([trongNhom]),
        });
        await campaignRepo.SaveChangesAsync();

        var lead = await service.CreateAsync(
            NewCreateDto("Khach") with { CampaignCode = "CD-2026-010", AssignedToUserId = chiDinh });

        Assert.Equal(chiDinh, lead.AssignedToUserId);
    }

    /// <summary>
    /// Mã sai thì lead VẪN VÀO ĐƯỢC, chỉ là không gắn chiến dịch. Ném lỗi ở đây nghĩa là form thu
    /// lead trả lỗi cho khách vì một cái mã gõ nhầm trong nội bộ công ty.
    /// </summary>
    [Fact]
    public async Task Ma_chien_dich_sai_thi_van_tao_duoc_lead()
    {
        var service = NewServiceCoChienDich(out _, out _);

        var lead = await service.CreateAsync(NewCreateDto("Khach") with { CampaignCode = "CD-KHONG-CO" });

        Assert.NotEqual(Guid.Empty, lead.Id);
        Assert.Null(lead.AssignedToUserId);
    }

    /// <summary>
    /// Người phụ trách ĐI THEO sang hồ sơ khách hàng. Không mang theo thì đúng lúc khách trở nên
    /// quan trọng nhất lại không ai biết hỏi ai — và người đã chăm nó suốt giai đoạn trước mất dấu.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_mang_theo_nguoi_phu_trach_sang_khach_hang()
    {
        var service = NewService(out var repo, out var customerRepo);
        var sale = Guid.NewGuid();
        var lead = new Lead { FullName = "Co Nguoi Phu Trach", Phone = "0911222333", AssignedToUserId = sale };
        await repo.AddAsync(lead);
        await repo.SaveChangesAsync();

        var kq = await service.ConvertAsync(lead.Id);

        var kh = await customerRepo.GetByIdAsync(kq.CustomerId);
        Assert.Contains(sale.ToString(), kh!.CrmProfileJson ?? "", StringComparison.Ordinal);
    }

    /// <summary>Chọn người khác ở hộp xác nhận thì BÀN GIAO — ghi đè người đang phụ trách lead.</summary>
    [Fact]
    public async Task ConvertAsync_chon_nguoi_khac_thi_ban_giao()
    {
        var service = NewService(out var repo, out var customerRepo);
        var cu = Guid.NewGuid();
        var moi = Guid.NewGuid();
        var lead = new Lead { FullName = "Ban Giao", Phone = "0911222444", AssignedToUserId = cu };
        await repo.AddAsync(lead);
        await repo.SaveChangesAsync();

        var kq = await service.ConvertAsync(lead.Id, moi);

        var kh = await customerRepo.GetByIdAsync(kq.CustomerId);
        Assert.Contains(moi.ToString(), kh!.CrmProfileJson ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain(cu.ToString(), kh.CrmProfileJson ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConvertAsync_already_converted_throws_ConflictException()
    {
        var service = NewService(out _, out _);
        var created = await service.CreateAsync(NewCreateDto("Le Van C"));
        await service.ConvertAsync(created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.ConvertAsync(created.Id));
    }

    [Fact]
    public async Task ListAsync_filters_by_status_and_q()
    {
        var service = NewService(out var repo, out _);
        await repo.AddAsync(new Lead { FullName = "Nguyễn Tiềm Năng", Source = "facebook", Status = LeadStatus.Qualified });
        await repo.AddAsync(new Lead { FullName = "Trần Mất", Source = "zalo", Status = LeadStatus.Lost });
        await repo.SaveChangesAsync();

        Assert.Equal("Nguyễn Tiềm Năng", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(Status: (int)LeadStatus.Qualified))).Items).FullName);
        Assert.Equal("Trần Mất", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(Q: "zalo"))).Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_branch()
    {
        var service = NewService(out var repo, out _);
        var branchA = Guid.NewGuid();
        var creator = Guid.NewGuid();
        await repo.AddAsync(new Lead { FullName = "A", BranchId = branchA, CreatedByUserId = creator });
        await repo.AddAsync(new Lead { FullName = "B", BranchId = Guid.NewGuid() });
        await repo.SaveChangesAsync();

        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(BranchId: branchA))).Items).FullName);
        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new LeadListFilter(CreatedByUserId: creator))).Items).FullName);
    }

    [Fact]
    public async Task GetStatsAsync_counts_by_status_and_converted()
    {
        var service = NewService(out var repo, out _);
        await repo.AddAsync(new Lead { FullName = "A", Status = LeadStatus.New });
        await repo.AddAsync(new Lead { FullName = "B", Status = LeadStatus.Qualified });
        await repo.AddAsync(new Lead { FullName = "C", Status = LeadStatus.Won, ConvertedCustomerId = Guid.NewGuid() });
        await repo.SaveChangesAsync();

        var stats = await service.GetStatsAsync();

        Assert.Equal(3, stats.Total);
        Assert.Equal(1, stats.New);
        Assert.Equal(1, stats.Qualified);
        Assert.Equal(1, stats.Won);
        Assert.Equal(1, stats.Converted);
        Assert.Equal(0, stats.Lost);
    }
}
