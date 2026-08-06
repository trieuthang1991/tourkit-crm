using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Pages.TourRatings;

// Đánh giá tour: DataTables SERVER-SIDE + GIỮ ĐỦ cột bản cũ
// (web/src/features/ratings/TourRatingsPage.tsx — ResourcePage: Chuyến đi · Khách hàng · Số sao · Trạng thái)
// + cột SĐT/Nhận xét đã có ở bản Razor. Service ListAsync(page,size) KHÔNG nhận filter/keyword nên
// màn này không có thanh lọc và ô tìm của DataTables bị ẩn (không lọc client, không get-all).
[Authorize(Policy = "rating.view")]
public class IndexModel : TkListPageModel
{
    private readonly ITourRatingService _svc;
    private readonly IDepartureService _departures;
    private readonly TourKit.Application.Common.IRepository<TourKit.Shared.Entities.Order> _orders;
    private readonly TourKit.Api.Services.UserDirectory _users;
    public IndexModel(ITourRatingService svc, IDepartureService departures,
        TourKit.Application.Common.IRepository<TourKit.Shared.Entities.Order> orders,
        TourKit.Api.Services.UserDirectory users)
    {
        _svc = svc;
        _departures = departures;
        _orders = orders;
        _users = users;
    }

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int Stars { get; set; } = 5;
        public string? Comment { get; set; }
        public int Status { get; set; } = 1;
        public Guid? SalesUserId { get; set; }
        public Guid? OperatorUserId { get; set; }
    }

    public TourKit.Application.Crm.Dtos.TourRatingStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];

    /// <summary>Nhãn trạng thái đánh giá (thay số thô): 0 Ẩn · 1 Hiển thị.</summary>
    public static string StatusLabel(int s) => s == 0 ? "Ẩn" : "Hiển thị";
    public static string StatusColor(int s) => s == 0 ? "secondary" : "success";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang. Lọc sao/trạng thái đẩy SQL.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var stars = int.TryParse(Request.Query["stars"], out var st) ? st : (int?)null;
        var status = int.TryParse(Request.Query["status"], out var stt) ? stt : (int?)null;
        var sales = Guid.TryParse(Request.Query["salesUserId"], out var su) ? su : (Guid?)null;
        var oper = Guid.TryParse(Request.Query["operatorUserId"], out var ou) ? ou : (Guid?)null;
        var result = await _svc.ListAsync(dt.Page, dt.Size, dt.Keyword, stars, status, sales, oper);

        // Enrich CHỈ các bản ghi TRONG TRANG (không nạp cả bảng): chuyến (tên+ngày+NVĐH), đơn (mã+NVPT), tên NV.
        var deps = new Dictionary<Guid, DepartureDto>();
        foreach (var id in result.Items.Where(r => r.TourDepartureId is not null).Select(r => r.TourDepartureId!.Value).Distinct())
        {
            try { deps[id] = await _departures.GetAsync(id); } catch (Exception) { /* chuyến đã xoá */ }
        }

        var orderIds = result.Items.Where(r => r.OrderId is not null).Select(r => r.OrderId!.Value).Distinct().ToList();
        var orders = orderIds.Count == 0
            ? []
            : (await _orders.ListAsync(o => orderIds.Contains(o.Id))).ToDictionary(o => o.Id, o => o);

        var userNames = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        string? Name(Guid? uid) => uid is { } id && userNames.TryGetValue(id, out var n) ? n : null;
        string Fmt(DateTimeOffset? d) => d?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—";

        var items = result.Items.Select(r =>
        {
            DepartureDto? dep = r.TourDepartureId is { } dgid && deps.TryGetValue(dgid, out var d) ? d : null;
            TourKit.Shared.Entities.Order? ord = r.OrderId is { } oid && orders.TryGetValue(oid, out var o) ? o : null;
            return new
            {
                id = r.Id,
                tourDepartureId = r.TourDepartureId,
                departureName = dep is null ? null : $"{dep.Code} — {dep.Title}",
                orderCode = ord?.Code,                                   // Mã đặt chỗ (bám staging)
                departureDate = Fmt(dep?.DepartureDate),                 // Ngày khởi hành
                returnDate = Fmt(dep?.EndDate),                          // Ngày về
                // NVPT/NVĐH lấy từ field thật của đánh giá (sửa/lọc được); rỗng thì thử suy từ đơn/chuyến.
                salesUserId = r.SalesUserId,
                operatorUserId = r.OperatorUserId,
                salesName = Name(r.SalesUserId) ?? Name(ord?.SalesUserId),
                operatorName = Name(r.OperatorUserId) ?? Name(dep?.AssignedToUserId),
                orderId = r.OrderId,
                customerName = r.CustomerName,
                customerPhone = r.CustomerPhone,
                stars = r.Stars,
                comment = r.Comment,
                status = r.Status,
                statusLabel = StatusLabel(r.Status),
                statusColor = StatusColor(r.Status),
            };
        }).ToList();

        return DtJson(dt.Draw, result.Total, result.Total, items);
    }

    /// <summary>Xuất CSV (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var stars = int.TryParse(Request.Query["stars"], out var st) ? st : (int?)null;
        var status = int.TryParse(Request.Query["status"], out var stt) ? stt : (int?)null;
        var result = await _svc.ListAsync(1, max, Request.Query["search"], stars, status);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Chuyến đi,Khách hàng,SĐT,Số sao,Nhận xét,Trạng thái");
        foreach (var r in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(r.TourDepartureId?.ToString())).Append(',').Append(C(r.CustomerName)).Append(',')
              .Append(C(r.CustomerPhone)).Append(',')
              .Append(r.Stars.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(C(r.Comment)).Append(',')
              .Append(r.Status.ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "danh-gia-tour.csv");
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateTourRatingDto(Input.CustomerName, Input.CustomerPhone, Input.Stars, Input.Comment, Input.Status,
                Input.SalesUserId, Input.OperatorUserId));
        }
        else
        {
            await _svc.CreateAsync(new CreateTourRatingDto(null, null, Input.CustomerName, Input.CustomerPhone, Input.Stars, Input.Comment, Input.Status,
                Input.SalesUserId, Input.OperatorUserId));
        }

        return new JsonResult(Result.Success("Đã lưu đánh giá tour."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá đánh giá tour.";
        return RedirectToPage();
    }
}
