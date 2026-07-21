using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.Invoices;

// Thêm/Sửa hoá đơn VAT — mirror Pages/Quotes/Edit: header + bảng DÒNG động (hoá đơn không tạo rỗng,
// Subtotal/VAT/Total do server tính lại từ dòng nên client chỉ hiển thị số dự trù).
// Ô "Đơn hàng" BẮT BUỘC có mặt: InvoiceService.UpdateAsync gán thẳng invoice.OrderId = dto.OrderId,
// thiếu ô này thì mỗi lần sửa hoá đơn sẽ XOÁ liên kết đơn hàng.
[Authorize(Policy = "invoice.manage")]
public class EditModel : PageModel
{
    private readonly IInvoiceService _svc;
    private readonly IBookingService _orders;

    public EditModel(IInvoiceService svc, IBookingService orders)
    {
        _svc = svc;
        _orders = orders;
    }

    private static readonly JsonSerializerOptions LinesJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InvoiceDto? Invoice { get; private set; }

    /// <summary>Nhãn đơn hàng đang gán — để prefill Select2 mà không phải gọi thêm 1 vòng ajax.</summary>
    public string? OrderLabel { get; private set; }

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public string LinesJson { get; set; } = "[]";

    public sealed class InputModel
    {
        public string? Series { get; set; }
        public string? Number { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập ngày hoá đơn")] public string InvoiceDate { get; set; } = "";
        public Guid? OrderId { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập tên người mua")] public string BuyerName { get; set; } = "";
        public string? BuyerTaxCode { get; set; }
        public string? BuyerAddress { get; set; }
        public int Status { get; set; }
        public string? Note { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id is Guid g && g != Guid.Empty)
        {
            try
            {
                Invoice = await _svc.GetAsync(g);
            }
            catch (Exception)
            {
                return NotFound();
            }

            Id = Invoice.Id;

            if (Invoice.OrderId is Guid oid)
            {
                try
                {
                    var order = await _orders.GetOrderAsync(oid);
                    OrderLabel = $"{order.Code} - {order.CustomerName ?? "—"}";
                }
                catch (Exception)
                {
                    // Đơn hàng đã bị xoá: vẫn giữ Id để không âm thầm cắt liên kết khi lưu lại.
                    OrderLabel = "(đơn hàng không còn tồn tại)";
                }
            }
        }

        return Page();
    }

    /// <summary>Nguồn Select2 tìm đơn hàng — chỉ 20 kết quả, không get-all.</summary>
    public async Task<IActionResult> OnGetOrderSearchAsync(string? q)
    {
        var result = await _orders.ListOrdersAsync(1, 20, new OrderListFilter(Q: q));
        var items = result.Items.Select(o => new
        {
            id = o.Id,
            text = $"{o.Code} - {o.CustomerName ?? "—"}",
        });
        return new JsonResult(new { results = items });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        CreateInvoiceLineDto[] lines;
        try
        {
            lines = JsonSerializer.Deserialize<CreateInvoiceLineDto[]>(LinesJson, LinesJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return new JsonResult(Result.Error("Danh sách dòng hoá đơn không hợp lệ."));
        }

        if (lines.Length == 0)
        {
            return new JsonResult(Result.Error("Cần ít nhất một dòng hàng hoá/dịch vụ."));
        }

        // Ngày hoá đơn là NGÀY NGHIỆP VỤ, không phải mốc thời gian: neo thẳng offset 0.
        // Nếu parse theo giờ VN rồi ToUniversalTime() thì 21/07 00:00+07 → 20/07 17:00Z và
        // màn hình hiển thị lùi mất một ngày.
        if (!DateTime.TryParseExact(Input.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            && !DateTime.TryParse(Input.InvoiceDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
        {
            return new JsonResult(Result.Error("Ngày hoá đơn không hợp lệ (dd/mm/yyyy)."));
        }

        var invoiceDate = new DateTimeOffset(day.Date, TimeSpan.Zero);

        var series = (Input.Series ?? "").Trim();
        var number = (Input.Number ?? "").Trim();

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                var updated = await _svc.UpdateAsync(g, new UpdateInvoiceDto(
                    series, number, invoiceDate, Input.OrderId,
                    Input.BuyerName, Input.BuyerTaxCode, Input.BuyerAddress, Input.Status, Input.Note, lines));
                return new JsonResult(Result.Success("Đã lưu hoá đơn.", new { id = updated.Id }));
            }

            var created = await _svc.CreateAsync(new CreateInvoiceDto(
                series, number, invoiceDate, Input.OrderId,
                Input.BuyerName, Input.BuyerTaxCode, Input.BuyerAddress, Input.Status, Input.Note, lines));
            return new JsonResult(Result.Success("Đã tạo hoá đơn.", new { id = created.Id }));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
