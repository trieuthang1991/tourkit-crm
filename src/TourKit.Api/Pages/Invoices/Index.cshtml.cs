using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.Invoices;

// Danh sách hoá đơn VAT: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/invoices/InvoicesPage.tsx): 6 KPI, thanh lọc (từ khoá + ngày HĐ từ/đến +
// trạng thái), cột kép (số/ký hiệu · người mua/MST · tổng tiền + VAT), dòng tổng cộng trang,
// export CSV, nút Xoá (invoice.manage).
// VẪN read-only cho Thêm/Sửa: IInvoiceService.Create/Update yêu cầu tập DÒNG hoá đơn động
// (Lines[] — hoá đơn không tạo rỗng, subtotal/VAT/total tính từ dòng); Pages/ chưa có màn Edit
// hoá đơn nên không dựng form ở đây để tránh bịa.
[Authorize(Policy = "invoice.view")]
public class IndexModel : TkListPageModel
{
    private readonly IInvoiceService _svc;
    public IndexModel(IInvoiceService svc) => _svc = svc;

    public InvoiceStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0);

    public bool CanManage => User.HasClaim("perm", "invoice.manage");

    // Trạng thái hoá đơn: 0 nháp · 1 đã phát hành · 2 đã huỷ.
    public static string StatusLabel(int s) => s switch
    {
        0 => "Nháp",
        1 => "Đã phát hành",
        2 => "Đã huỷ",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "success",
        2 => "danger",
        _ => "secondary",
    };

    public async Task OnGetAsync() => Stats = await _svc.GetStatsAsync();

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí InvoiceListFilter hỗ trợ.</summary>
    private InvoiceListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new InvoiceListFilter(
            Q: keyword,
            Status: I("status"),
            DateFrom: D("dateFrom"),
            DateTo: D("dateTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var items = result.Items.Select(x => new
        {
            id = x.Id,
            series = string.IsNullOrWhiteSpace(x.Series) ? "—" : x.Series,
            number = string.IsNullOrWhiteSpace(x.Number) ? "—" : x.Number,
            invoiceDate = x.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            buyerName = string.IsNullOrWhiteSpace(x.BuyerName) ? "—" : x.BuyerName,
            buyerTaxCode = x.BuyerTaxCode,
            totalAmount = x.TotalAmount,
            vatAmount = x.VatAmount,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new
        {
            amount = items.Sum(x => x.totalAmount),
            vat = items.Sum(x => x.vatAmount),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>Thẻ thống kê dạng JSON — làm tươi KPI sau khi xoá.</summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var s = await _svc.GetStatsAsync();
        return new JsonResult(new
        {
            total = s.Total,
            totalAmount = s.TotalAmount,
            totalVat = s.TotalVat,
            issued = s.Issued,
            draft = s.Draft,
            cancelled = s.Cancelled,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var keyword = Request.Query["search"].ToString() is { Length: > 0 } s ? s : null;
        var result = await _svc.ListAsync(1, max, BuildFilter(keyword));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Số hoá đơn,Ký hiệu,Ngày,Người mua,Mã số thuế,Tổng tiền,VAT,Trạng thái");
        foreach (var i in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(i.Number)).Append(',').Append(C(i.Series)).Append(',')
              .Append(C(i.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(C(i.BuyerName)).Append(',').Append(C(i.BuyerTaxCode)).Append(',')
              .Append(i.TotalAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(i.VatAmount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(i.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "hoa-don.csv");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá hoá đơn."));
        }

        try
        {
            await _svc.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá hoá đơn."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
