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
        // Ngày "đến" tính hết ngày: người dùng chọn 31/08 là có ý gồm cả hôm đó, còn mốc thô sẽ cắt
        // ở 00:00 và làm rơi mất mọi cơ hội tạo trong ngày cuối kỳ.
        Den = Parse(den)?.AddDays(1).AddTicks(-1);

        TheoNhanVien = await svc.ReportByUserAsync(Tu, Den);
        LyDoHuy = await svc.ReportCancelReasonsAsync(Tu, Den);

        // Tên người tra ở TẦNG NÀY, không ở tầng dịch vụ: danh bạ có cache 60 giây theo tenant, còn
        // service thì không biết gì về nó.
        var ten = await users.NamesAsync();
        TheoNhanVien = TheoNhanVien
            .Select(r => r with { UserName = ten.GetValueOrDefault(r.UserId) ?? "(không rõ)" })
            .ToList();
    }

    private static DateTimeOffset? Parse(string? raw) =>
        DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, out var d) ? d : null;
}
