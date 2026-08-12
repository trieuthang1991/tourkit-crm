using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Services;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog;
using TourKit.Application.Common;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Api.Pages.SalesOpportunities;

/// <summary>
/// Cơ hội bán hàng (hệ cũ: <c>/booking-ticket</c>). Lưới server-side + bảng kanban theo CỘT CẤU HÌNH
/// ĐƯỢC, khác màn Lead cũ vốn cứng 5 trạng thái.
///
/// Đừng nhầm với màn Lead: Lead là số khách thô chia cho sale, cơ hội là một nhu cầu có tiền.
/// </summary>
[Authorize(Policy = "opportunity.view")]
public class IndexModel : TkListPageModel
{
    private readonly ISalesOpportunityService _svc;
    private readonly UserDirectory _users;
    private readonly IBranchService _branches;
    private readonly ICustomerSourceService _sources;
    private readonly IMarketTypeService _markets;
    private readonly ITransferReasonService _reasons;
    private readonly ITourTemplateService _templates;
    private readonly IDepartureService _departures;
    private readonly IBookingService _booking;

    public IndexModel(
        ISalesOpportunityService svc,
        UserDirectory users,
        IBranchService branches,
        ICustomerSourceService sources,
        IMarketTypeService markets,
        ITransferReasonService reasons,
        ITourTemplateService templates,
        IDepartureService departures,
        IBookingService booking)
    {
        _svc = svc;
        _users = users;
        _branches = branches;
        _sources = sources;
        _markets = markets;
        _reasons = reasons;
        _templates = templates;
        _departures = departures;
        _booking = booking;
    }

    public SalesOpportunityStatsDto Stats { get; private set; } = new(0, 0m, 0m, 0, 0, new Dictionary<int, int>());
    public IReadOnlyList<OpportunityStageDto> Stages { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Branches { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Sources { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Markets { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> CancelReasons { get; private set; } = [];

    /// <summary>Mẫu tour cho ô chọn trong form. Nạp sẵn: đây là danh mục có biên (số tuyến công ty bán).</summary>
    public IReadOnlyList<(Guid Id, string Label)> Templates { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "opportunity.manage");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã cơ hội")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên cơ hội")] public string Title { get; set; } = "";
        public string? Content { get; set; }

        [Required(ErrorMessage = "Bắt buộc nhập tên khách")] public string ContactName { get; set; } = "";
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactAddress { get; set; }
        public Guid? CustomerId { get; set; }

        public int AdultQty { get; set; }
        public int ChildQty { get; set; }
        public int ChildSmallQty { get; set; }
        public int BabyQty { get; set; }

        public decimal PriceAdult { get; set; }
        public decimal PriceChild { get; set; }
        public decimal PriceChildSmall { get; set; }
        public decimal PriceBaby { get; set; }

        public Guid? TemplateId { get; set; }
        public Guid? TourDepartureId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? CustomerSourceId { get; set; }
        public Guid? MarketTypeId { get; set; }
        public bool FromWebsite { get; set; }

        /// <summary>Người phụ trách — nhiều người. Ô select2 nhiều lựa chọn gửi lên nhiều giá trị cùng tên.</summary>
        public List<Guid> AssigneeIds { get; set; } = [];
        public List<Guid> FollowerIds { get; set; } = [];
    }

    public async Task OnGetAsync()
    {
        Stages = await _svc.ListStagesAsync();
        Stats = await _svc.GetStatsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
        Branches = (await _branches.ListAsync()).Select(b => (b.Id, b.Name)).ToList();
        Sources = (await _sources.ListAsync()).Select(s => (s.Id, s.Name)).ToList();
        Markets = (await _markets.ListAsync()).Select(m => (m.Id, m.Name)).ToList();
        CancelReasons = (await _reasons.ListAsync()).Select(r => (r.Id, r.Name)).ToList();
        Templates = (await _templates.ListAsync(1, TranDanhMuc.TourMau)).Items
            .Select(t => (t.Id, $"{t.Code} — {t.Title}")).ToList();
    }

    private SalesOpportunityListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;
        DateTimeOffset? D(string k) =>
            DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new SalesOpportunityListFilter(
            Q: keyword,
            StageCode: I("stageCode"),
            AssigneeUserId: G("assigneeUserId"),
            CustomerId: G("customerId"),
            TemplateId: G("templateId"),
            CustomerSourceId: G("customerSourceId"),
            MarketTypeId: G("marketTypeId"),
            BranchId: G("branchId"),
            CreatedByUserId: G("createdByUserId"),
            FromWebsite: B("fromWebsite"),
            IsConfirmed: B("isConfirmed"),
            CreatedFrom: D("createdFrom"),
            CreatedTo: D("createdTo"));
    }

    /// <summary>
    /// Nạp danh sách cột phễu cho các handler DỮ LIỆU.
    ///
    /// Cần thiết vì <see cref="OnGetAsync"/> chỉ chạy khi tải trang, không chạy cho
    /// <c>?handler=Data</c>. Thiếu bước này thì <see cref="Dong"/> tra tên cột trong danh sách rỗng
    /// và mọi dòng hiện "—" ở ô trạng thái — hỏng im lặng, lưới vẫn chạy bình thường.
    /// </summary>
    private async Task NapCotAsync() => Stages = await _svc.ListStagesAsync();

    /// <summary>Nhãn chuyến/mẫu tour của ĐÚNG trang hiện tại — mỗi loại một câu, không phải mỗi dòng một câu.</summary>
    private Dictionary<Guid, string> _nhanChuyen = [];
    private Dictionary<Guid, string> _nhanMau = [];

    private async Task NapNhanAsync(IEnumerable<SalesOpportunityDto> ds)
    {
        var idChuyen = ds.Where(o => o.TourDepartureId is not null)
            .Select(o => o.TourDepartureId!.Value).Distinct().ToList();
        _nhanChuyen = (await _departures.ByIdsAsync(idChuyen))
            .ToDictionary(d => d.Id, d => $"{d.Code} — {d.Title}");

        // Mẫu tour là danh mục có biên nên lấy cả bảng một lần rồi tra trong bộ nhớ vẫn rẻ hơn hỏi
        // lại theo từng tập id ở mỗi lần phân trang.
        _nhanMau = (await _templates.ListAsync(1, TranDanhMuc.TourMau)).Items
            .ToDictionary(t => t.Id, t => $"{t.Code} — {t.Title}");
    }

    public async Task<IActionResult> OnGetDataAsync()
    {
        await NapCotAsync();
        var dt = ParseDataTables();
        var loc = BuildFilter(dt.Keyword);
        var kq = await _svc.ListAsync(dt.Page, dt.Size, loc);

        // Thẻ thống kê tính THEO BỘ LỌC đang áp, không phải toàn bảng: người dùng lọc xuống một chi
        // nhánh mà thẻ vẫn hiện số toàn công ty thì hai con số trên cùng màn hình đá nhau.
        var tk = await _svc.GetStatsAsync(loc);

        await NapNhanAsync(kq.Items);
        var data = kq.Items.Select(o => Dong(o)).ToList();

        return GridJson(dt, tk.Total, kq.Total, data, new Dictionary<string, object?>
        {
            ["stats"] = new
            {
                total = tk.Total,
                tongGiaTri = tk.TongGiaTri,
                giaTriDangMo = tk.GiaTriDangMo,
                daChot = tk.DaChot,
                daHuy = tk.DaHuy,
                theoCot = tk.TheoCot,
            },
            // Tổng của TRANG hiện tại cho dòng tổng dưới lưới.
            ["pageSum"] = new { giaTri = kq.Items.Sum(o => o.EstimatedValue) },
        });
    }

    /// <summary>Một cơ hội theo id — cho đường dẫn sâu <c>/co-hoi?mo={id}</c>.</summary>
    public async Task<IActionResult> OnGetOneAsync(Guid id)
    {
        await NapCotAsync();
        try
        {
            var mot = await _svc.GetAsync(id);
            await NapNhanAsync([mot]);
            return new JsonResult(Dong(mot));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Một CỘT kanban: top-N cơ hội của đúng một cột, kèm bộ lọc đang áp, có "Tải thêm".
    /// Không get-all — mỗi cột tự tải trang của riêng nó.
    /// </summary>
    public async Task<IActionResult> OnGetKanbanColumnAsync(int stageCode, int page = 1, int size = 15)
    {
        await NapCotAsync();
        var loc = BuildFilter(null) with { StageCode = stageCode };
        var kq = await _svc.ListAsync(page, size <= 0 || size > 50 ? 15 : size, loc);
        await NapNhanAsync(kq.Items);

        return new JsonResult(new
        {
            stageCode,
            total = kq.Total,
            hasMore = page * kq.Size < kq.Total,
            // Khoá "cards" giữ đúng tên với kanban của màn Lead — hai màn dùng chung lối dựng thẻ,
            // đổi tên khoá ở đây chỉ tạo thêm một biến thể phải nhớ.
            cards = kq.Items.Select(o => Dong(o)).ToList(),
        });
    }

    /// <summary>
    /// Xuất CSV theo ĐÚNG bộ lọc đang áp — không phải toàn bảng. Xuất ra một file khác với thứ đang
    /// nhìn thấy là cách nhanh nhất khiến người dùng hết tin vào chức năng này.
    /// </summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var kq = await _svc.ListAsync(1, max, BuildFilter(keyword));
        await NapCotAsync();
        await NapNhanAsync(kq.Items);

        var users = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã,Tên cơ hội,Khách,SĐT,Số khách,Giá trị dự kiến,Cột phễu,Mẫu tour,Chuyến,Phụ trách,Ngày tạo");
        foreach (var o in kq.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            var phuTrach = string.Join("; ", o.Assignees.Where(a => !a.IsFollower)
                .Select(a => users.TryGetValue(a.UserId, out var n) ? n : ""));
            var cot = Stages.FirstOrDefault(x => x.Code == o.StageCode)?.Name ?? "";
            var mau = o.TemplateId is Guid tid && _nhanMau.TryGetValue(tid, out var tn) ? tn : "";
            var chuyen = o.TourDepartureId is Guid did && _nhanChuyen.TryGetValue(did, out var dn) ? dn : "";

            sb.Append(C(o.Code)).Append(',').Append(C(o.Title)).Append(',')
              .Append(C(o.ContactName)).Append(',').Append(C(o.ContactPhone)).Append(',')
              .Append(o.AdultQty + o.ChildQty + o.ChildSmallQty + o.BabyQty).Append(',')
              .Append(o.EstimatedValue.ToString("0", CultureInfo.InvariantCulture)).Append(',')
              .Append(C(cot)).Append(',').Append(C(mau)).Append(',').Append(C(chuyen)).Append(',')
              .Append(C(phuTrach)).Append(',')
              .Append(C(o.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).AppendLine();
        }

        // Có BOM để Excel bản Việt mở ra không vỡ dấu — thiếu nó thì mọi tên khách thành ký tự lạ.
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "co-hoi-ban-hang.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa cơ hội."));
        }

        // Gộp hai ô chọn người thành một danh sách: người phụ trách (IsFollower = false) và người
        // theo dõi (true). Cùng một người có thể đứng cả hai vai.
        var nguoi = Input.AssigneeIds.Select(u => new OpportunityAssigneeDto(u, false))
            .Concat(Input.FollowerIds.Select(u => new OpportunityAssigneeDto(u, true)))
            .ToList();

        try
        {
            if (Id is { } id)
            {
                await _svc.UpdateAsync(id, new UpdateSalesOpportunityDto(
                    Input.Code, Input.Title, Input.Content,
                    Input.ContactName, Input.ContactPhone, Input.ContactEmail, Input.ContactAddress,
                    Input.CustomerId,
                    Input.AdultQty, Input.ChildQty, Input.ChildSmallQty, Input.BabyQty,
                    Input.PriceAdult, Input.PriceChild, Input.PriceChildSmall, Input.PriceBaby,
                    Input.TemplateId, Input.TourDepartureId,
                    nguoi, Input.BranchId,
                    Input.CustomerSourceId, Input.MarketTypeId, Input.FromWebsite));
                return new JsonResult(Result.Success("Đã lưu cơ hội."));
            }

            await _svc.CreateAsync(new CreateSalesOpportunityDto(
                Input.Code, Input.Title, Input.Content,
                Input.ContactName, Input.ContactPhone, Input.ContactEmail, Input.ContactAddress,
                Input.CustomerId,
                Input.AdultQty, Input.ChildQty, Input.ChildSmallQty, Input.BabyQty,
                Input.PriceAdult, Input.PriceChild, Input.PriceChildSmall, Input.PriceBaby,
                Input.TemplateId, Input.TourDepartureId,
                nguoi, Input.BranchId,
                Input.CustomerSourceId, Input.MarketTypeId, Input.FromWebsite));
            return new JsonResult(Result.Success("Đã tạo cơ hội."));
        }
        catch (ValidationAppException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
        catch (ConflictException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
        catch (NotFoundException)
        {
            return new JsonResult(Result.Error("Không tìm thấy cơ hội."));
        }
    }

    /// <summary>
    /// Chuyển cột (kéo thẻ kanban hoặc chọn ở form). Lý do huỷ đi kèm ngay tại đây — tách thành hai
    /// lần gọi thì có khoảng giữa mà cơ hội đã huỷ nhưng chưa có lý do, đúng thứ báo cáo cần.
    /// </summary>
    public async Task<IActionResult> OnPostMoveAsync(
        Guid id, int? stageCode, int? to, Guid? cancelReasonId, string? cancelNote)
    {
        // Nhận CẢ HAI tên tham số: bảng kanban dùng chung gửi "to", còn hộp lý do huỷ và các lời gọi
        // khác gửi "stageCode". Bắt kanban đổi theo mình thì phải sửa cả bộ dùng chung của mọi màn.
        var cot = to ?? stageCode;
        if (cot is not { } cotDich)
        {
            return new JsonResult(Result.Error("Thiếu cột đích."));
        }

        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền chuyển cột."));
        }

        await NapCotAsync();
        try
        {
            var sau = await _svc.MoveStageAsync(id, new MoveOpportunityStageDto(cotDich, cancelReasonId, cancelNote));
            await NapNhanAsync([sau]);
            return new JsonResult(Result.Success("Đã chuyển cột.", Dong(sau)));
        }
        catch (ValidationAppException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
        catch (ConflictException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
        catch (NotFoundException)
        {
            return new JsonResult(Result.Error("Không tìm thấy cơ hội."));
        }
    }

    /// <summary>
    /// Chốt cơ hội thành ĐƠN THẬT.
    ///
    /// Đi qua <see cref="IBookingService.CreateBookingAsync"/> — cùng đường với màn chi tiết chuyến —
    /// chứ KHÔNG tự dựng Order ở đây. Một đường tạo đơn thứ hai sẽ lệch luật sức chứa và giá với
    /// đường đang có, và lệch âm thầm: đơn vẫn tạo được, chỉ là vượt chỗ hoặc sai giá.
    ///
    /// Việc đánh dấu cơ hội đã chốt nằm TRONG BookingService, sau khi đơn đã lưu — đúng thứ tự của
    /// hệ cũ. Ở đây chỉ kiểm những thứ người dùng phải bổ sung trước.
    /// </summary>
    public async Task<IActionResult> OnPostChotDonAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền chốt đơn."));
        }

        SalesOpportunityDto o;
        try
        {
            o = await _svc.GetAsync(id);
        }
        catch (NotFoundException)
        {
            return new JsonResult(Result.Error("Không tìm thấy cơ hội."));
        }

        if (o.ConvertedOrderId is not null)
        {
            return new JsonResult(Result.Error("Cơ hội này đã chốt thành đơn rồi."));
        }

        // Nói RÕ thiếu gì thay vì chỉ báo "không chốt được": hai thứ này người dùng bổ sung được
        // ngay trong form, còn thông báo chung chung thì họ phải tự đoán.
        if (o.TourDepartureId is not { } chuyenId)
        {
            return new JsonResult(Result.Error("Chưa chọn chuyến khởi hành cho cơ hội này — mở sửa và chọn chuyến trước."));
        }

        if (o.CustomerId is not { } khachId)
        {
            return new JsonResult(Result.Error("Chưa gắn khách hàng cho cơ hội này — mở sửa và chọn khách trước."));
        }

        if (o.AdultQty + o.ChildQty + o.ChildSmallQty + o.BabyQty <= 0)
        {
            return new JsonResult(Result.Error("Cơ hội chưa có khách nào — nhập số khách trước khi chốt."));
        }

        try
        {
            // Giá lấy từ CƠ HỘI, không lấy giá niêm yết của mẫu tour: cơ hội là thứ đã thoả thuận với
            // khách, chốt xong mà đơn mang giá khác là sai ngay tại lúc bàn giao.
            var don = await _booking.CreateBookingAsync(
                chuyenId,
                new CreateBookingDto(khachId, o.AdultQty, o.ChildQty, o.ChildSmallQty, o.BabyQty, OpportunityId: id),
                new SeatPrices(o.PriceAdult, o.PriceChild, o.PriceChildSmall, o.PriceBaby));

            return new JsonResult(Result.Success($"Đã chốt đơn {don.Code}.", new { orderId = don.Id }));
        }
        catch (ValidationAppException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
        catch (ConflictException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá cơ hội."));
        }

        try
        {
            await _svc.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá cơ hội."));
        }
        catch (ConflictException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
        catch (NotFoundException)
        {
            return new JsonResult(Result.Error("Không tìm thấy cơ hội."));
        }
    }

    /// <summary>
    /// Một dòng gửi xuống trình duyệt. Dùng CHUNG cho lưới, kanban và đường dẫn sâu — ba nơi lệch
    /// hình dạng dữ liệu là kiểu lỗi đã gặp: mở sửa từ kanban thì form trắng vài ô.
    /// </summary>
    private object Dong(SalesOpportunityDto o) => new
    {
        id = o.Id,
        code = o.Code,
        title = o.Title,
        content = o.Content,
        contactName = o.ContactName,
        contactPhone = o.ContactPhone,
        contactEmail = o.ContactEmail,
        contactAddress = o.ContactAddress,
        customerId = o.CustomerId,
        adultQty = o.AdultQty,
        childQty = o.ChildQty,
        childSmallQty = o.ChildSmallQty,
        babyQty = o.BabyQty,
        priceAdult = o.PriceAdult,
        priceChild = o.PriceChild,
        priceChildSmall = o.PriceChildSmall,
        priceBaby = o.PriceBaby,
        estimatedValue = o.EstimatedValue,
        soKhach = o.AdultQty + o.ChildQty + o.ChildSmallQty + o.BabyQty,
        templateId = o.TemplateId,
        templateName = o.TemplateId is Guid tid && _nhanMau.TryGetValue(tid, out var tn) ? tn : null,
        tourDepartureId = o.TourDepartureId,
        departureLabel = o.TourDepartureId is Guid did && _nhanChuyen.TryGetValue(did, out var dn) ? dn : null,
        stageCode = o.StageCode,
        stageName = Stages.FirstOrDefault(s => s.Code == o.StageCode)?.Name ?? "—",
        // Badge của lưới dùng bảng màu Bootstrap, không dùng mã màu tự do của danh mục: hai cột
        // "Huỷ"/"Chốt đơn" phải luôn đỏ/xanh dù người dùng đặt màu gì cho cột của họ.
        stageTone = o.StageCode == OpportunityStageCode.ChotDon ? "success"
            : o.StageCode == OpportunityStageCode.Huy ? "danger" : "info",
        cancelReasonId = o.CancelReasonId,
        cancelNote = o.CancelNote,
        convertedOrderId = o.ConvertedOrderId,
        // Tách sẵn hai vai để form nạp thẳng vào hai ô chọn, không bắt trình duyệt lọc lại.
        assigneeIds = o.Assignees.Where(a => !a.IsFollower).Select(a => a.UserId).ToList(),
        followerIds = o.Assignees.Where(a => a.IsFollower).Select(a => a.UserId).ToList(),
        branchId = o.BranchId,
        customerSourceId = o.CustomerSourceId,
        marketTypeId = o.MarketTypeId,
        fromWebsite = o.FromWebsite,
        isConfirmed = o.IsConfirmed,
        createdAtText = o.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
    };
}
