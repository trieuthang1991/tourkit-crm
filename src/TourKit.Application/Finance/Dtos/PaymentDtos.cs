namespace TourKit.Application.Finance.Dtos;

/// <summary>DTO tạo phiếu chi — tiền công ty chi trả cho NCC theo một đơn (đối xứng phiếu thu).</summary>
public sealed record CreatePaymentDto(
    Guid? ProviderId, Guid? OrderCostId, decimal Amount, string PaymentMethod,
    string? Partner, string? ReceiverName, string? Note);

/// <summary>DTO phiếu chi trả ra cho client (không lộ entity).</summary>
public sealed record PaymentDto(
    Guid Id, string Code, Guid OrderId, Guid? ProviderId, Guid? OrderCostId,
    decimal Amount, string PaymentMethod, DateTimeOffset IssuedAt,
    string? Partner, string? ReceiverName, string? Note, int Status, bool IsRecognized);
