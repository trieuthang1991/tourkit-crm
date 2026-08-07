namespace TourKit.Application.Crm.Dtos;

public sealed record CustomerCareDto(
    Guid Id, Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt,
    string? Feedback, Guid? AssignedToUserId, int Status,
    string? CustomerName = null, string? AssigneeName = null);

/// <summary>Bộ lọc chăm sóc KH: khách · người phụ trách · trạng thái · từ khoá (tiêu đề). Status: 0 mới, 1 đang xử lý, 2 hoàn thành.</summary>
// RemindFrom/RemindTo: khoảng NGÀY NHẮC HẸN (dùng cho bảng lịch hẹn xếp cột theo thời gian).
// RemindIsNull: true = chỉ lấy lịch CHƯA đặt ngày nhắc; false = chỉ lấy lịch đã có ngày.
public sealed record CustomerCareListFilter(
    string? Q = null, Guid? CustomerId = null, Guid? AssignedToUserId = null, int? Status = null,
    DateTimeOffset? RemindFrom = null, DateTimeOffset? RemindTo = null, bool? RemindIsNull = null,
    // ExcludeStatus: bỏ hẳn một trạng thái. Cột "Quá hạn" cần "chưa hoàn thành" = mọi trạng thái
    // TRỪ Hoàn thành — Status đơn lẻ không diễn tả được điều đó.
    int? ExcludeStatus = null);

/// <summary>Thẻ thống kê đầu màn Chăm sóc KH: tổng · mới · đang xử lý · hoàn thành · quá hạn nhắc.</summary>
public sealed record CustomerCareStatsDto(int Total, int New, int InProgress, int Done, int Overdue);

public sealed record CreateCustomerCareDto(
    Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, Guid? AssignedToUserId, int Status);

public sealed record UpdateCustomerCareDto(
    string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status);
