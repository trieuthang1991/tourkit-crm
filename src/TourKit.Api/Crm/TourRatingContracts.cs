namespace TourKit.Api.Crm;

/// <summary>DTO tạo đánh giá sau tour mới.</summary>
public sealed record CreateTourRatingRequest(Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);

/// <summary>DTO cập nhật đánh giá sau tour.</summary>
public sealed record UpdateTourRatingRequest(string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record TourRatingResponse(Guid Id, Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);
