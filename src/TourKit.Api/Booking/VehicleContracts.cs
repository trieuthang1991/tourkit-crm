namespace TourKit.Api.Booking;

/// <summary>DTO tạo xe mới.</summary>
public sealed record CreateVehicleRequest(string Name, string? FirmName, int SeatType, int Status);

/// <summary>DTO cập nhật xe.</summary>
public sealed record UpdateVehicleRequest(string Name, string? FirmName, int SeatType, int Status);

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record VehicleResponse(Guid Id, string Name, string? FirmName, int SeatType, int Status);
