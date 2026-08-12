using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.Shared;

/// <summary>
/// Base cho màn danh sách phân trang từ server. Hiểu CẢ hai kiểu request:
/// DataTables (start/length/search[value]) và Tabulator qua tk.grid (page/size/q).
/// Nhờ vậy chuyển một màn từ DataTables sang tk.grid chỉ cần đổi hàm trả JSON.
/// </summary>
public abstract class TkListPageModel : PageModel
{
    protected sealed record DtRequest(int Draw, int Start, int Length, string Search, int? GridPage, int? GridSize)
    {
        public int Page => GridPage ?? (Length <= 0 ? 1 : (Start / Length) + 1);
        public int Size => GridSize ?? (Length <= 0 ? 20 : Length);
        public string? Keyword => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }

    protected DtRequest ParseDataTables()
    {
        var q = Request.Query;
        int P(string k, int def) => int.TryParse(q[k], out var n) ? n : def;
        int? N(string k, int max) => int.TryParse(q[k], out var n) && n > 0 && n <= max ? n : null;

        // tk.grid gửi page/size/q; DataTables gửi start/length/search[value]. Ưu tiên cái nào có mặt.
        var search = q["search[value]"].ToString();
        if (string.IsNullOrWhiteSpace(search)) { search = q["q"].ToString(); }
        return new DtRequest(P("draw", 0), P("start", 0), P("length", 20), search,
            N("page", 100_000), N("size", 500));
    }

    /// <summary>
    /// JSON cho danh sách server-side. Trả luôn cả khoá của Tabulator (last_page/last_row) nên
    /// màn nào chuyển từ DataTables sang tk.grid CHỈ cần sửa .cshtml, không phải đụng handler.
    /// </summary>
    protected JsonResult DtJson(int draw, int recordsTotal, int recordsFiltered, object data)
        => GridJson(ParseDataTables(), recordsTotal, recordsFiltered, data);

    /// <summary>
    /// JSON phục vụ ĐỒNG THỜI Tabulator (tk.grid: last_page + data) và DataTables
    /// (draw/recordsTotal/recordsFiltered/data) — hai bộ khoá không đụng nhau nên một payload
    /// dùng được cho cả hai, chuyển màn sang lưới mới không sợ vỡ màn cũ hay test cũ.
    /// <paramref name="extra"/> để gắn thêm số liệu riêng của màn (ví dụ pageSum của dòng tổng).
    /// </summary>
    protected JsonResult GridJson(DtRequest dt, int recordsTotal, int recordsFiltered, object data,
        IDictionary<string, object?>? extra = null)
    {
        var size = dt.Size <= 0 ? 20 : dt.Size;
        var payload = new Dictionary<string, object?>
        {
            ["last_page"] = Math.Max(1, (int)Math.Ceiling(recordsFiltered / (double)size)),
            ["last_row"] = recordsFiltered,
            ["draw"] = dt.Draw,
            ["recordsTotal"] = recordsTotal,
            ["recordsFiltered"] = recordsFiltered,
            ["data"] = data,
        };
        if (extra is not null)
        {
            foreach (var kv in extra) { payload[kv.Key] = kv.Value; }
        }
        return new JsonResult(payload);
    }

    /// <summary>
    /// Tra nhà cung cấp cho ô chọn gọi server (<c>?handler=ProviderLookup</c>).
    ///
    /// Đặt ở LỚP CƠ SỞ vì ô chọn NCC có mặt ở 6 màn (vé đoàn, vé lẻ, quỹ vé, quỹ phòng, đặt dịch vụ,
    /// điều hành). Sáu bản sao cùng một hàm chắc chắn sẽ lệch nhau — đúng chuyện vừa xảy ra với danh
    /// mục tỉnh thành, khi hai danh sách cùng nghĩa tồn tại song song và một cái đã lỗi thời.
    ///
    /// Vì sao là PAGE HANDLER chứ không phải controller <c>/api/v1/...</c>: scheme "smart" trong
    /// Program.cs ép mọi đường dẫn bắt đầu bằng /api sang JWT Bearer, nên cookie của trình duyệt bị
    /// bỏ qua và select2 nhận 401. Handler của trang đi đường cookie như mọi màn khác.
    ///
    /// Quyền lấy từ chính trang đang mở: trang nào cũng đã có [Authorize] riêng, nên tới được đây
    /// nghĩa là người dùng đã qua cửa quyền của màn đó.
    /// </summary>
    public async Task<IActionResult> OnGetProviderLookupAsync(string? q, int? type)
    {
        var svc = HttpContext.RequestServices
            .GetRequiredService<TourKit.Application.Providers.IProviderService>();

        // 20 dòng mỗi lượt: đây là ô gợi ý, không phải danh sách để đọc — gõ thêm vài ký tự nhanh
        // hơn cuộn qua trăm dòng.
        var kq = await svc.ListAsync(1, 20, new TourKit.Application.Providers.Dtos.ProviderListFilter(Q: q, Type: type));

        // Kèm mã vào nhãn: nhiều NCC trùng tên (chuỗi khách sạn ở các tỉnh khác nhau), chỉ hiện tên
        // thì người dùng chọn nhầm mà không có gì phân biệt.
        return new JsonResult(new
        {
            results = kq.Items.Select(p => new
            {
                id = p.Id,
                text = string.IsNullOrWhiteSpace(p.Code) ? p.Name : $"{p.Name} ({p.Code})",
            }),
        });
    }

    /// <summary>
    /// Tra KHÁCH HÀNG THEO SỐ ĐIỆN THOẠI (<c>?handler=KhachTheoSdt</c>).
    ///
    /// Khách hàng định danh bằng SĐT — hai khách khác nhau không được cùng một số (luật ở
    /// <c>CustomerService</c>). Nên ở MỌI form có liên quan tới khách, việc đầu tiên phải là hỏi
    /// "số này đã có ai chưa": có thì lấy đúng hồ sơ đó, chưa có thì tạo mới.
    ///
    /// Không làm vậy thì mỗi form lại đẻ ra một khách trùng số, và tới lúc đối soát công nợ mới phát
    /// hiện một người có ba hồ sơ — lúc đó gộp lại rất tốn công vì đơn hàng đã bám vào cả ba.
    ///
    /// Trả về CẢ HAI: danh sách gợi ý theo số đang gõ dở, và bản ghi khớp chính xác (nếu có). Chỉ trả
    /// bản khớp chính xác thì người dùng phải nhớ trọn mười chữ số mới tra được; chỉ trả danh sách
    /// thì gõ đủ số rồi vẫn phải bấm chọn một dòng duy nhất — thừa một thao tác ở mọi lần nhập.
    ///
    /// Đặt ở LỚP CƠ SỞ vì mọi màn danh sách đều có thể cần: cơ hội, đơn hàng, báo giá, đặt dịch vụ.
    /// </summary>
    public async Task<IActionResult> OnGetKhachTheoSdtAsync(string? sdt)
    {
        var svc = HttpContext.RequestServices
            .GetRequiredService<TourKit.Application.Customers.ICustomerService>();

        var so = (sdt ?? string.Empty).Trim();
        if (so.Length < 3)
        {
            return new JsonResult(new { results = Array.Empty<object>(), khop = (object?)null });
        }

        // GỢI Ý theo số đang gõ dở, không đợi gõ đủ: người dùng nhớ "khách này số đuôi 888" là ra
        // được ngay, khỏi phải nhớ trọn mười chữ số. ListAsync đã tra theo PhoneNormalized nên
        // 0912… và +84912… ra cùng một người.
        var goiY = await svc.ListAsync(1, 8, new TourKit.Application.Customers.Dtos.CustomerListFilter(Q: so));

        // Khớp CHÍNH XÁC thì client gắn luôn, khỏi bắt chọn lại một dòng duy nhất.
        var khop = await svc.FindByPhoneAsync(so);

        return new JsonResult(new
        {
            results = goiY.Items.Select(c => new
            {
                id = c.Id,
                code = c.Code,
                fullName = c.FullName,
                phone = c.Phone,
                email = c.Email,
            }),
            khop = khop is null ? null : new
            {
                id = khop.Id,
                code = khop.Code,
                fullName = khop.FullName,
                phone = khop.Phone,
                email = khop.Email,
            },
        });
    }

    /// <summary>
    /// TẠO NHANH khách ngay trong form đang mở (<c>?handler=TaoNhanhKhach</c>).
    ///
    /// Bắt người dùng rời form hiện tại, sang màn Khách hàng tạo hồ sơ rồi quay lại là cách chắc
    /// chắn nhất khiến họ bỏ dở việc đang làm — hoặc gõ đại một cái tên vào ô chữ để đi tiếp, và
    /// thế là mất luôn liên kết tới hồ sơ khách.
    ///
    /// Chỉ nhận đúng những gì form liên kết biết: tên, số, email. Phần hồ sơ đầy đủ (nguồn, phân
    /// loại, thẻ…) để người dùng bổ sung sau ở màn Khách hàng — nhồi hết vào đây thì "tạo nhanh"
    /// không còn nhanh.
    /// </summary>
    public async Task<IActionResult> OnPostTaoNhanhKhachAsync(string? fullName, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new JsonResult(TourKit.Api.Web.Result.Error("Bắt buộc nhập tên khách."));
        }

        var svc = HttpContext.RequestServices
            .GetRequiredService<TourKit.Application.Customers.ICustomerService>();

        try
        {
            var moi = await svc.CreateAsync(new TourKit.Application.Customers.Dtos.CreateCustomerDto(
                fullName.Trim(), string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                Email: string.IsNullOrWhiteSpace(email) ? null : email.Trim()));

            return new JsonResult(TourKit.Api.Web.Result.Success($"Đã tạo khách {moi.FullName}.", new
            {
                id = moi.Id,
                code = moi.Code,
                fullName = moi.FullName,
                phone = moi.Phone,
                email = moi.Email,
            }));
        }
        catch (TourKit.Application.Common.ConflictException ex)
        {
            // Luật chặn trùng SĐT nằm ở tầng dịch vụ nên bắt được cả trường hợp hai người cùng bấm
            // "Tạo nhanh" một lúc — thông báo của service đã nêu tên khách đang giữ số đó.
            return new JsonResult(TourKit.Api.Web.Result.Error(ex.Message));
        }
        catch (TourKit.Application.Common.ValidationAppException ex)
        {
            return new JsonResult(TourKit.Api.Web.Result.Error(ex.Message));
        }
    }

    /// <summary>
    /// Tra chuyến khởi hành cho ô chọn gọi server (<c>?handler=DepartureLookup</c>).
    ///
    /// Cùng lý do đặt ở lớp cơ sở như <see cref="OnGetProviderLookupAsync"/>: ô chọn chuyến có ở 4 màn
    /// (phân công HDV, điều xe, xe chờ điều, báo giá).
    ///
    /// Khác NCC ở một điểm đáng nói: nhà cung cấp, xe, đại lý là DANH MỤC — đông tới mấy rồi cũng
    /// dừng lại. Chuyến đi thì mỗi tháng một dày thêm và không bao giờ giảm, nên đây là ô duy nhất
    /// chắc chắn sẽ vượt trần nếu cứ nạp sẵn. Vượt trần lại hỏng IM LẶNG: ô vẫn hiện, chỉ thiếu lựa
    /// chọn, người dùng kết luận nhầm là chưa có chuyến đó.
    ///
    /// Nhãn ghép "MÃ — Tên (dd/MM/yyyy)": nhiều chuyến trùng tên tuyến, chỉ khác ngày đi.
    /// </summary>
    public async Task<IActionResult> OnGetDepartureLookupAsync(string? q)
    {
        var svc = HttpContext.RequestServices
            .GetRequiredService<TourKit.Application.Booking.IDepartureService>();

        var ds = await svc.LookupAsync(q, 20);

        return new JsonResult(new
        {
            results = ds.Select(d => new
            {
                id = d.Id,
                text = d.DepartureDate is { } ngay
                    ? $"{d.Code} — {d.Title} ({ngay:dd/MM/yyyy})"
                    : $"{d.Code} — {d.Title}",
            }),
        });
    }
}
