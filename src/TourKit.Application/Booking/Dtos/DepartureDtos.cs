namespace TourKit.Application.Booking.Dtos;

public sealed record DepartureDto(
    Guid Id, string Code, string Title, Guid? TemplateId,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots, int Status,
    string? TourType = null, Guid? AssignedToUserId = null, bool IsClosed = false,
    // Bám cột staging quản lý chuyến (/sample-tours): Giá (từ template) · Giữ/Bán/Còn chỗ · Ngày đóng chỗ.
    decimal Price = 0m, int SeatHeld = 0, int SeatSold = 0, int SeatRemaining = 0,
    DateTimeOffset? ClosedAt = null, int? Category = null);

/// <summary>Bộ lọc danh sách chuyến khởi hành (bám hệ cũ). IsClosed = đã đóng/chốt sổ.</summary>
public sealed record DepartureListFilter(
    string? Q = null, string? TourType = null, int? Status = null,
    Guid? AssignedToUserId = null, bool? IsClosed = null,
    DateTimeOffset? DepartureFrom = null, DateTimeOffset? DepartureTo = null,
    int? Category = null,   // Loại sản phẩm tour (FIT/GIT/LandTour…) — cho màn quản lý chuyến theo loại
    // Bám thêm bộ lọc staging /sample-tours (chỉ field CÓ ở model chuyến): khoảng ngày kết thúc + sắp xếp.
    DateTimeOffset? EndFrom = null, DateTimeOffset? EndTo = null,
    string? Sort = null);   // dateAsc | dateDesc(mặc định) | slots | code

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
