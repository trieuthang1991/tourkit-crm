namespace TourKit.Application.Booking.Dtos;

public sealed record DepartureDto(
    Guid Id, string Code, string Title, Guid? TemplateId,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots, int Status);

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
