namespace TourKit.Application.Booking.Dtos;

public sealed record DepartureDto(
    Guid Id, string Code, string Title, Guid? TemplateId,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots, int Status,
    string? TourType = null, Guid? AssignedToUserId = null, bool IsClosed = false);

/// <summary>Bộ lọc danh sách chuyến khởi hành (bám hệ cũ). IsClosed = đã đóng/chốt sổ.</summary>
public sealed record DepartureListFilter(
    string? Q = null, string? TourType = null, int? Status = null,
    Guid? AssignedToUserId = null, bool? IsClosed = null,
    DateTimeOffset? DepartureFrom = null, DateTimeOffset? DepartureTo = null);

/// <summary>Thẻ thống kê đầu màn Chuyến đi: tổng chuyến + sắp khởi hành + đã đóng + tổng chỗ.</summary>
public sealed record DepartureStatsDto(int Total, int Upcoming, int Closed, int TotalSlots);

/// <summary>Tuỳ chọn lọc động màn Chuyến đi (loại tour lấy từ dữ liệu thật).</summary>
public sealed record DepartureFilterOptionsDto(IReadOnlyList<string> TourTypes);

public sealed record CreateDepartureDto(
    Guid? TemplateId, string Code, string Title,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots);

/// <summary>Một ngày khởi hành trong lô mở hàng loạt.</summary>
public sealed record BatchDepartureItemDto(DateTimeOffset DepartureDate, DateTimeOffset? EndDate);

/// <summary>Mở hàng loạt chuyến từ 1 mẫu tour (legacy BatchCreateTour): mỗi ngày → 1 chuyến,
/// Code = CodePrefix-STT. TotalSlots=0 → kế thừa mẫu.</summary>
public sealed record BatchCreateDeparturesDto(
    Guid TemplateId, string CodePrefix, string? Title, int TotalSlots, BatchDepartureItemDto[] Items);

public sealed record BatchCreateResultDto(int Created, DepartureDto[] Departures);
