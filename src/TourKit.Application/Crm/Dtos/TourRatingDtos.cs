namespace TourKit.Application.Crm.Dtos;

public sealed record TourRatingDto(
    Guid Id, Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone,
    int Stars, string? Comment, int Status, Guid? SalesUserId = null, Guid? OperatorUserId = null);

public sealed record CreateTourRatingDto(
    Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status,
    Guid? SalesUserId = null, Guid? OperatorUserId = null);

public sealed record UpdateTourRatingDto(
    string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status,
    Guid? SalesUserId = null, Guid? OperatorUserId = null);

/// <summary>Tổng hợp đánh giá theo từng chuyến đi (màn "Feedback theo Tour"): số lượt + sao trung bình.</summary>
public sealed record TourRatingByTourDto(Guid TourDepartureId, int RatingCount, double AverageStars);

/// <summary>Thẻ thống kê màn Đánh giá tour (bám staging): tổng · điểm TB · đếm theo bậc sao 5→1.</summary>
public sealed record TourRatingStatsDto(
    int Total, double AverageStars, int Star5, int Star4, int Star3, int Star2, int Star1);
