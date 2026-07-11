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
    int Status);

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
