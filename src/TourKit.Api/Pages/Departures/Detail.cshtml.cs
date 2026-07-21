using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Departures;

// Chi tiết chuyến đi — bám web/src/features/booking/DepartureDetailPage.tsx:
// thẻ thông tin chuyến + nút Đóng chuyến (departure.close) + khung đặt chỗ (booking.create)
// với 2 hành động của hệ cũ: "Chốt ngay" (CreateBooking) và "Giữ chỗ" (CreateHold).
// Bổ sung so với bản cũ: bảng ĐƠN HÀNG của chuyến — server-side, lọc bằng OrderListFilter.DepartureId
// (bản cũ không có khung này; thêm vào thì không phải rời màn để biết chuyến đã bán cho ai).
[Authorize(Policy = "departure.view")]
public class DetailModel : TkListPageModel
{
    private readonly IDepartureService _svc;
    private readonly IBookingService _booking;
    private readonly ICustomerService _customers;

    public DetailModel(IDepartureService svc, IBookingService booking, ICustomerService customers)
    {
        _svc = svc;
        _booking = booking;
        _customers = customers;
    }

    public DepartureDto? Departure { get; private set; }

    public bool CanClose => User.HasClaim("perm", "departure.close");
    public bool CanBook => User.HasClaim("perm", "booking.create");

    public string DepartureDateText => Departure?.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";
    public string EndDateText => Departure?.EndDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Departure = await _svc.GetAsync(id);
        }
        catch (Exception)
        {
            return NotFound();
        }

        return Page();
    }

    /// <summary>Nguồn Select2 tìm khách hàng — 20 kết quả mỗi lượt, không get-all.</summary>
    public async Task<IActionResult> OnGetCustomerSearchAsync(string? q)
    {
        var result = await _customers.ListAsync(1, 20, new CustomerListFilter(Q: q));
        var items = result.Items.Select(c => new
        {
            id = c.Id,
            text = string.IsNullOrWhiteSpace(c.Phone) ? c.FullName : $"{c.FullName} - {c.Phone}",
        });
        return new JsonResult(new { results = items });
    }

    /// <summary>Đơn hàng thuộc chuyến này — DataTables server-side, lọc tại DB theo DepartureId.</summary>
    public async Task<IActionResult> OnGetOrdersAsync(Guid id)
    {
        var dt = ParseDataTables();
        var result = await _booking.ListOrdersAsync(dt.Page, dt.Size, new OrderListFilter(Q: dt.Keyword, DepartureId: id));

        var data = result.Items.Select(o => new
        {
            id = o.Id,
            code = o.Code,
            customerName = o.CustomerName ?? "—",
            statusLabel = TourKit.Api.Pages.Orders.IndexModel.StatusLabel(o.Status),
            statusColor = TourKit.Api.Pages.Orders.IndexModel.StatusColor(o.Status),
            totalRevenue = o.TotalRevenue,
            amountPaid = o.AmountPaid,
            outstanding = o.Outstanding,
            seatTotal = o.SeatTotal,
            seatHeld = o.SeatHeld,
            seatSold = o.SeatSold,
        }).ToList();

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = result.Total,
            recordsFiltered = result.Total,
            data,
            // Tổng của TRANG hiện tại + số chỗ đã dùng trên toàn chuyến (recordsTotal = số đơn).
            pageSum = new
            {
                revenue = data.Sum(x => x.totalRevenue),
                paid = data.Sum(x => x.amountPaid),
                outstanding = data.Sum(x => x.outstanding),
                seats = data.Sum(x => x.seatTotal),
            },
        });
    }

    /// <summary>Đặt chỗ: action = "book" (chốt ngay) hoặc "hold" (giữ chỗ) — đúng 2 nút hệ cũ.</summary>
    public async Task<IActionResult> OnPostBookAsync(Guid id, string action, Guid customerId, int adultQty, int childQty, int childSmallQty, int babyQty)
    {
        if (!CanBook)
        {
            return new JsonResult(Result.Error("Bạn không có quyền đặt chỗ."));
        }

        if (customerId == Guid.Empty)
        {
            return new JsonResult(Result.Error("Chưa chọn khách hàng."));
        }

        if (adultQty + childQty + childSmallQty + babyQty <= 0)
        {
            return new JsonResult(Result.Error("Cần nhập ít nhất 1 khách."));
        }

        var dto = new CreateBookingDto(customerId, adultQty, childQty, childSmallQty, babyQty);

        try
        {
            if (string.Equals(action, "hold", StringComparison.OrdinalIgnoreCase))
            {
                var seat = await _booking.CreateHoldAsync(id, dto);
                return new JsonResult(Result.Success($"Đã giữ chỗ (mã {seat.ReservationCode ?? seat.Id.ToString()}).", new { orderId = seat.OrderId }));
            }

            var order = await _booking.CreateBookingAsync(id, dto);
            return new JsonResult(Result.Success($"Đã chốt đơn {order.Code}.", new { orderId = order.Id }));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id)
    {
        if (!CanClose)
        {
            return new JsonResult(Result.Error("Bạn không có quyền đóng chuyến."));
        }

        try
        {
            await _svc.CloseAsync(id);
            return new JsonResult(Result.Success("Đã đóng chuyến đi."));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }
}
