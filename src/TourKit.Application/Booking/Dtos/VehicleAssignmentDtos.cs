namespace TourKit.Application.Booking.Dtos;

public sealed record VehicleAssignmentDto(
    Guid Id,
    Guid TourDepartureId,
    Guid VehicleId,
    string? DriverName,
    string? DriverPhone,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    string? Note,
    int Status,
    string? VehicleName = null,       // tên xe (+ số chỗ)
    string? DepartureTitle = null,
    string? DepartureCode = null);

/// <summary>Bộ lọc lịch điều xe (bám hệ cũ): xe · chuyến · trạng thái · khoảng ngày đón.</summary>
public sealed record VehicleAssignmentListFilter(
    Guid? VehicleId = null, Guid? DepartureId = null, int? Status = null,
    DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null);

/// <summary>Thẻ thống kê đầu màn Lịch điều xe: tổng + theo trạng thái + số xe.</summary>
public sealed record VehicleAssignmentStatsDto(int Total, int Created, int Active, int VehicleCount);

public sealed record CreateVehicleAssignmentDto(
    Guid TourDepartureId,
    Guid VehicleId,
    string? DriverName,
    string? DriverPhone,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    string? Note,
    int Status);

public sealed record UpdateVehicleAssignmentDto(
    Guid VehicleId,
    string? DriverName,
    string? DriverPhone,
    DateTimeOffset? TimeGo,
    DateTimeOffset? TimeCome,
    string? Note,
    int Status);
