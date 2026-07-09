namespace TourKit.Application.Booking.Dtos;

public sealed record VehicleDto(Guid Id, string Name, string? FirmName, int SeatType, int Status);

public sealed record CreateVehicleDto(string Name, string? FirmName, int SeatType, int Status);

public sealed record UpdateVehicleDto(string Name, string? FirmName, int SeatType, int Status);
