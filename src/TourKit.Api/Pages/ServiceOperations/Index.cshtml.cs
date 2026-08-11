using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Providers;

namespace TourKit.Api.Pages.ServiceOperations;

// Phiếu điều hành dịch vụ: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/serviceOperations/ServiceOperationsPage.tsx): 4 KPI đếm theo trạng thái chi,
// tab Tất cả/Chờ chi/Chưa chi hết/Thành công, lọc từ khoá + NCC, cột ghép Phiếu ĐH và Dịch vụ/NCC,
// cột Thanh toán (đã TT + còn thiếu), dòng Tổng cộng, offcanvas "Ghi nhận thanh toán" (PayAsync).
// READ-ONLY ngoài PayAsync: IServiceOperationService là lớp đọc trên ServiceBooking.
[Authorize(Policy = "servicebooking.view")]
public class IndexModel : TkListPageModel
{
    private readonly IServiceOperationService _svc;
    private readonly IProviderService _providers;

    public IndexModel(IServiceOperationService svc, IProviderService providers)
    {
        _svc = svc;
        _providers = providers;
    }

    public ServiceOperationStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Providers { get; private set; } = [];

    [BindProperty] public Guid Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải >= 0")] public decimal PaidAmount { get; set; }
    }

    public static string PaymentLabel(int s) => s switch
    {
        0 => "Chờ chi",
        1 => "Chưa chi hết",
        2 => "Hoàn thành",
        _ => "—",
    };

    public static string PaymentColor(int s) => s switch
    {
        2 => "success",
        1 => "warning",
        _ => "danger",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        // Ô chọn NCC nay gọi server (?handler=ProviderLookup) nên không nạp danh mục xuống trang nữa.
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí ServiceOperationListFilter hỗ trợ.</summary>
    private ServiceOperationListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        return new ServiceOperationListFilter(
            Q: keyword,
            ProviderId: Guid.TryParse(q["providerId"], out var g) ? g : null,
            PaymentStatus: int.TryParse(q["paymentStatus"], out var s) ? s : null);
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + KPI/tổng cộng theo bộ lọc đang áp.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var filter = BuildFilter(dt.Keyword);
        var result = await _svc.ListAsync(dt.Page, dt.Size, filter);
        var stats = await _svc.GetStatsAsync(filter);

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            code = x.Code,
            providerName = string.IsNullOrWhiteSpace(x.ProviderName) ? "—" : x.ProviderName,
            description = string.IsNullOrWhiteSpace(x.Description) ? "—" : x.Description,
            usageDateText = x.UsageDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            totalAmount = x.TotalAmount,
            paidAmount = x.PaidAmount,
            recognizedPaidAmount = x.RecognizedPaidAmount,
            remainingAmount = x.RemainingAmount,
            paymentStatus = x.PaymentStatus,
            statusLabel = PaymentLabel(x.PaymentStatus),
            statusColor = PaymentColor(x.PaymentStatus),
        }).ToList();

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            // Thẻ KPI + dòng tổng cộng bám bộ lọc đang áp (đúng như stats hệ cũ nhận filter).
            stats = new
            {
                total = stats.Total,
                unpaid = stats.Unpaid,
                partial = stats.Partial,
                done = stats.Done,
                totalCost = stats.TotalCost,
                totalPaid = stats.TotalPaid,
                totalRemaining = stats.TotalRemaining,
            },
        });
    }

    public async Task<IActionResult> OnPostPayAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            await _svc.PayAsync(Id, new PayServiceOperationDto(Input.PaidAmount));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã ghi nhận thanh toán."));
    }
}
