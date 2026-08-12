namespace TourKit.Application.Sales.Dtos;

/// <summary>Một cột của phễu Cơ hội (danh mục cấu hình được).</summary>
public sealed record OpportunityStageDto(Guid Id, int Code, string Name, int SortOrder, string? Color, bool IsSystem);

/// <summary>Người phụ trách / người theo dõi gắn với một cơ hội.</summary>
public sealed record OpportunityAssigneeDto(Guid UserId, bool IsFollower);

public sealed record SalesOpportunityDto(
    Guid Id, string Code, string Title, string? Content,
    string ContactName, string? ContactPhone, string? ContactEmail, string? ContactAddress,
    Guid? CustomerId,
    int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    decimal EstimatedValue,
    Guid? TemplateId, Guid? TourDepartureId,
    int StageCode,
    Guid? CancelReasonId, string? CancelNote,
    Guid? ConvertedOrderId,
    IReadOnlyList<OpportunityAssigneeDto> Assignees,
    Guid? CreatedByUserId, Guid? BranchId,
    Guid? CustomerSourceId, Guid? MarketTypeId, bool FromWebsite,
    bool IsConfirmed, DateTimeOffset? ConfirmedAt, Guid? ConfirmedByUserId,
    string? AttachmentIds,
    DateTimeOffset CreatedAt);

/// <summary>
/// Bộ lọc màn Cơ hội. <paramref name="AssigneeUserId"/> là tiêu chí ĐẮT NHẤT của màn này ("cơ hội
/// của tôi") — chính là thứ hệ cũ không lọc nổi vì nhét id vào cột chuỗi.
/// </summary>
public sealed record SalesOpportunityListFilter(
    string? Q = null,
    int? StageCode = null,
    Guid? AssigneeUserId = null,
    Guid? CustomerId = null,
    Guid? TemplateId = null,
    Guid? CustomerSourceId = null,
    Guid? MarketTypeId = null,
    Guid? BranchId = null,
    Guid? CreatedByUserId = null,
    bool? FromWebsite = null,
    bool? IsConfirmed = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null);

/// <summary>
/// Thẻ thống kê đầu màn. <paramref name="TheoCot"/> đếm theo TỪNG cột phễu (khoá = StageCode) chứ
/// không đếm cứng 5-6 bậc: cột do người dùng cấu hình nên không biết trước có bao nhiêu.
/// </summary>
public sealed record SalesOpportunityStatsDto(
    int Total,
    decimal TongGiaTri,
    decimal GiaTriDangMo,
    int DaChot,
    int DaHuy,
    IReadOnlyDictionary<int, int> TheoCot);

public sealed record CreateSalesOpportunityDto(
    string Code, string Title, string? Content,
    string ContactName, string? ContactPhone, string? ContactEmail, string? ContactAddress,
    Guid? CustomerId,
    int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    Guid? TemplateId, Guid? TourDepartureId,
    IReadOnlyList<OpportunityAssigneeDto>? Assignees = null,
    Guid? BranchId = null,
    Guid? CustomerSourceId = null, Guid? MarketTypeId = null, bool FromWebsite = false,
    string? AttachmentIds = null);

/// <summary>
/// Sửa cơ hội. KHÔNG có StageCode: chuyển cột đi qua <c>MoveStageAsync</c> vì bước đó kèm luật riêng
/// (huỷ phải có lý do, chốt đơn không đặt tay). Để chung vào đây thì lưu form bình thường cũng lách
/// được cả hai luật.
/// </summary>
public sealed record UpdateSalesOpportunityDto(
    string Code, string Title, string? Content,
    string ContactName, string? ContactPhone, string? ContactEmail, string? ContactAddress,
    Guid? CustomerId,
    int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    Guid? TemplateId, Guid? TourDepartureId,
    IReadOnlyList<OpportunityAssigneeDto>? Assignees = null,
    Guid? BranchId = null,
    Guid? CustomerSourceId = null, Guid? MarketTypeId = null, bool FromWebsite = false,
    string? AttachmentIds = null);

/// <summary>Chuyển cơ hội sang cột khác. <paramref name="CancelReasonId"/> bắt buộc khi sang cột Huỷ.</summary>
public sealed record MoveOpportunityStageDto(int StageCode, Guid? CancelReasonId = null, string? CancelNote = null);
