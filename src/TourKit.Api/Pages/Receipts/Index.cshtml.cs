using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Catalog;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Pages.Receipts;

// Danh sách phiếu thu: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/finance/ReceiptsListPage.tsx): 5 KPI, thanh lọc 9 tiêu chí (từ khoá, ngày,
// hình thức, số tiền từ/đến, chi nhánh, NV phụ trách, trạng thái), cột kép, dòng tổng cộng trang,
// export CSV, modal chi tiết + duyệt/từ chối tại chỗ.
// Vẫn KHÔNG có form tạo phiếu: IReceiptService.CreateAsync gắn theo orderId (tạo từ màn đơn).
[Authorize(Policy = "receipt.view")]
public class IndexModel : TkListPageModel
{
    private readonly IReceiptService _svc;
    private readonly IBranchService _branches;
    private readonly IUserAdminService _users;

    public IndexModel(IReceiptService svc, IBranchService branches, IUserAdminService users)
    {
        _svc = svc;
        _branches = branches;
        _users = users;
    }

    public ReceiptStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Branches { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];

    public bool CanApprove => User.HasClaim("perm", "receipt.approve");

    // Trạng thái phiếu thu: 0 chờ duyệt · 1 đã duyệt · 2 từ chối.
    public static string StatusLabel(int s) => s switch
    {
        0 => "Chờ duyệt",
        1 => "Đã duyệt",
        2 => "Từ chối",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "success",
        2 => "danger",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Branches = (await _branches.ListAsync()).Select(b => (b.Id, b.Name)).ToList();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng 9 tiêu chí ReceiptListFilter hỗ trợ.</summary>
    private ReceiptListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        decimal? M(string k) => decimal.TryParse(q[k], NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();

        return new ReceiptListFilter(
            Q: keyword,
            Status: I("status"),
            From: D("from"), To: D("to"),
            PaymentMethod: S("paymentMethod"),
            AmountFrom: M("amountFrom"), AmountTo: M("amountTo"),
            BranchId: G("branchId"),
            SalesUserId: G("salesUserId"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAllAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var items = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code,
            orderId = x.OrderId,
            orderCode = string.IsNullOrWhiteSpace(x.OrderCode) ? "—" : x.OrderCode,
            customerName = string.IsNullOrWhiteSpace(x.CustomerName) ? "—" : x.CustomerName,
            amount = x.Amount,
            paymentMethod = string.IsNullOrWhiteSpace(x.PaymentMethod) ? "—" : x.PaymentMethod,
            issuedAt = x.IssuedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            partner = string.IsNullOrWhiteSpace(x.Partner) ? "—" : x.Partner,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
            isRecognized = x.IsRecognized,
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI (bám dòng summary hệ cũ).
        var pageSum = new { amount = items.Sum(x => x.amount) };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>Thẻ thống kê dạng JSON — làm tươi KPI sau khi duyệt/từ chối.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync();
        return new JsonResult(new { total = s.Total, totalAmount = s.TotalAmount, pending = s.Pending, approved = s.Approved, rejected = s.Rejected });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAllAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("STT,Mã phiếu,Ngày,Khách hàng,Mã đơn,Người nộp,Hình thức,Số tiền,Trạng thái");
        var i = 0;
        foreach (var r in result.Items)
        {
            i++;
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(r.Code)).Append(',')
              .Append(C(r.IssuedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(r.CustomerName)).Append(',').Append(C(r.OrderCode)).Append(',')
              .Append(C(r.Partner)).Append(',').Append(C(r.PaymentMethod)).Append(',')
              .Append(r.Amount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(r.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "phieu-thu.csv");
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id) => await ActAsync(id, approve: true);

    public async Task<IActionResult> OnPostRejectAsync(Guid id) => await ActAsync(id, approve: false);

    private async Task<IActionResult> ActAsync(Guid id, bool approve)
    {
        if (!CanApprove)
        {
            return new JsonResult(Result.Error("Bạn không có quyền duyệt phiếu thu."));
        }

        try
        {
            if (approve)
            {
                await _svc.ApproveAsync(id);
                return new JsonResult(Result.Success("Đã duyệt phiếu thu."));
            }

            await _svc.RejectAsync(id);
            return new JsonResult(Result.Success("Đã từ chối phiếu thu."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
