using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales;

/// <summary>
/// Cơ hội bán hàng (hệ cũ: "Cơ hội bán hàng" — <c>BookingTicket</c>). Phễu từ lúc khách hỏi tới lúc
/// chốt thành đơn. Đừng nhầm với <c>ILeadService</c>: Lead là số khách thô chia cho sale.
/// </summary>
public interface ISalesOpportunityService
{
    Task<PagedResult<SalesOpportunityDto>> ListAsync(int page, int size, SalesOpportunityListFilter? filter = null);
    Task<SalesOpportunityStatsDto> GetStatsAsync(SalesOpportunityListFilter? filter = null);
    Task<SalesOpportunityDto> GetAsync(Guid id);
    Task<SalesOpportunityDto> CreateAsync(CreateSalesOpportunityDto dto);
    Task<SalesOpportunityDto> UpdateAsync(Guid id, UpdateSalesOpportunityDto dto);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Chuyển cơ hội sang cột khác trong phễu. Tách khỏi <see cref="UpdateAsync"/> vì kèm hai luật:
    /// sang cột Huỷ thì BẮT BUỘC có lý do, và cột Chốt đơn thì KHÔNG đặt tay được — nó do luồng đặt
    /// chỗ đánh dấu khi sinh đơn thật từ cơ hội.
    /// </summary>
    Task<SalesOpportunityDto> MoveStageAsync(Guid id, MoveOpportunityStageDto dto);

    /// <summary>Các cột của phễu, đã sắp theo thứ tự hiển thị — nguồn cho lưới lẫn bảng kanban.</summary>
    Task<IReadOnlyList<OpportunityStageDto>> ListStagesAsync();

    /// <summary>
    /// Cơ hội theo NGƯỜI PHỤ TRÁCH trong khoảng thời gian (hệ cũ <c>uspReportBookingTicket</c>).
    /// Một cơ hội có nhiều người phụ trách thì tính cho TỪNG người — đây là báo cáo hiệu suất cá
    /// nhân, chia đôi công theo đầu người sẽ làm mọi con số lẻ và không ai đối chiếu được.
    /// </summary>
    Task<IReadOnlyList<OpportunityByUserRowDto>> ReportByUserAsync(DateTimeOffset? tu, DateTimeOffset? den);

    /// <summary>Thống kê LÝ DO MẤT KHÁCH (hệ cũ <c>StatisticsCancelReason</c>) — đếm kèm giá trị đã mất.</summary>
    Task<IReadOnlyList<OpportunityCancelReasonRowDto>> ReportCancelReasonsAsync(DateTimeOffset? tu, DateTimeOffset? den);
}
