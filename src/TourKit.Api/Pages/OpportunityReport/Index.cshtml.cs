using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Services;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Api.Pages.OpportunityReport;

/// <summary>
/// Báo cáo phễu Cơ hội — gộp hai báo cáo của hệ cũ vào một màn:
/// <c>uspReportBookingTicket</c> (hiệu suất theo nhân viên) và <c>StatisticsCancelReason</c>
/// (lý do mất khách).
///
/// Để chung một màn vì người quản lý luôn đọc chúng cạnh nhau: bảng trên nói ai chốt được, bảng
/// dưới nói vì sao phần còn lại không chốt. Tách hai trang thì phải nhớ mở cả hai mới hiểu.
/// </summary>
[Authorize(Policy = "opportunity.view")]
public class IndexModel(ISalesOpportunityService svc, UserDirectory users) : PageModel
{
    public IReadOnlyList<OpportunityByUserRowDto> TheoNhanVien { get; private set; } = [];
    public IReadOnlyList<OpportunityCancelReasonRowDto> LyDoHuy { get; private set; } = [];

    /// <summary>Ngày NGƯỜI DÙNG chọn, giữ nguyên để đổ lại vào ô ngày. Cố tình KHÔNG quy về UTC:
    /// view in ra bằng <c>ToString("yyyy-MM-dd")</c> nên mốc UTC sẽ hiện LÙI một ngày ở giờ VN
    /// (chọn 08/09 mà ô ngày hiện 07/09). Mốc để truy vấn tính riêng trong <see cref="OnGetAsync"/>.</summary>
    public DateTimeOffset? Tu { get; private set; }
    public DateTimeOffset? Den { get; private set; }

    public int TongCoHoi => TheoNhanVien.Sum(x => x.Tong);
    public int TongChot => TheoNhanVien.Sum(x => x.DaChot);
    public decimal TongGiaTriMat => LyDoHuy.Sum(x => x.GiaTriMat);

    /// <summary>Tỉ lệ chốt CHUNG — tính lại từ tổng, không lấy trung bình các dòng: người giữ 100 cơ
    /// hội và người giữ 2 cơ hội không thể có cùng trọng số.</summary>
    public double TyLeChotChung => TongCoHoi == 0 ? 0d : Math.Round(TongChot * 100d / TongCoHoi, 1);

    public async Task OnGetAsync(string? tu, string? den)
    {
        Tu = Parse(tu);
        Den = Parse(den);

        // Mốc đem đi TRUY VẤN, tách khỏi mốc đem đi HIỂN THỊ ở trên.
        // - Quy về UTC: Npgsql chỉ nhận offset 0 cho cột timestamptz, đưa thẳng giờ VN (+07:00)
        //   xuống là ném ArgumentException và cả trang 500.
        // - Ngày "đến" tính HẾT ngày: người dùng chọn 11/09 là có ý gồm cả hôm đó, còn mốc thô sẽ
        //   cắt ở 00:00 và làm rơi mọi cơ hội tạo trong ngày cuối kỳ.
        // Cộng ngày TRƯỚC rồi mới quy đổi, để biên vẫn bám ngày VN: 23:59:59 ngày 11 giờ VN.
        var tuUtc = Tu?.ToUniversalTime();
        var denUtc = Den?.AddDays(1).AddTicks(-1).ToUniversalTime();

        TheoNhanVien = await svc.ReportByUserAsync(tuUtc, denUtc);
        LyDoHuy = await svc.ReportCancelReasonsAsync(tuUtc, denUtc);

        // Tên người tra ở TẦNG NÀY, không ở tầng dịch vụ: danh bạ có cache 60 giây theo tenant, còn
        // service thì không biết gì về nó.
        var ten = await users.NamesAsync();
        TheoNhanVien = TheoNhanVien
            .Select(r => r with { UserName = ten.GetValueOrDefault(r.UserId) ?? "(không rõ)" })
            .ToList();
    }

    /// <summary>
    /// Đọc ô ngày dạng "2026-09-08". Chuỗi KHÔNG kèm múi giờ nên .NET gắn offset MÁY CHỦ
    /// (VN = +07:00) — giữ nguyên như vậy, phần quy đổi để nơi gọi tự lo.
    ///
    /// <c>CreatedAt</c> là MỐC THỜI GIAN THẬT (giờ tạo bản ghi, lưu UTC) chứ không phải ngày nghiệp
    /// vụ, nên biên truy vấn phải quy về UTC — KHÔNG dùng
    /// <see cref="Shared.TkDate.Day(DateTimeOffset)"/> ở đây.
    /// </summary>
    private static DateTimeOffset? Parse(string? raw) =>
        DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, out var d) ? d : null;
}
