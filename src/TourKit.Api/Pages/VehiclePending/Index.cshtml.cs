using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.VehiclePending;

// READ-ONLY: nghiệp vụ phân xe KHÔNG có luồng duyệt riêng — Status bám legacy State (1=Created, 2=Active,
// 4=Delete), không có mã "chờ duyệt". VehicleAssignmentListFilter có Status nên "chờ duyệt" được diễn giải
// = Status 1 (Đã điều, CHƯA kích hoạt/đưa vào thực hiện). Chỉ hiển thị danh sách; kích hoạt/sửa làm ở màn Lịch điều xe.
[Authorize(Policy = "vehicle.view")]
public class IndexModel : PageModel
{
    private const int PendingStatus = 1; // Created = chờ đưa vào vận hành (chưa Active)
    private readonly IVehicleAssignmentService _svc;
    public IndexModel(IVehicleAssignmentService svc) => _svc = svc;

    public IReadOnlyList<VehicleAssignmentDto> Items { get; private set; } = [];

    public async Task OnGetAsync() =>
        Items = (await _svc.ListAsync(1, 1000, new VehicleAssignmentListFilter(Status: PendingStatus))).Items;
}
