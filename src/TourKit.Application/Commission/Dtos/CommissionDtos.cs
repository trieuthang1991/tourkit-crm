namespace TourKit.Application.Commission.Dtos;

/// <summary>Lợi nhuận đơn: doanh thu, chi phí (Σ ActualAmount các dòng OrderCost), lợi nhuận.</summary>
public sealed record OrderProfitDto(decimal Revenue, decimal Cost, decimal Profit);

/// <summary>DTO tạo chia hoa hồng/lợi nhuận cho 1 user theo đơn.</summary>
public sealed record CreateProfitShareDto(Guid UserId, decimal Percentage);

/// <summary>DTO trả ra cho client (không lộ entity).</summary>
public sealed record ProfitShareDto(
    Guid Id, Guid OrderId, Guid UserId, decimal Percentage, decimal Amount, decimal ProfitBase);
