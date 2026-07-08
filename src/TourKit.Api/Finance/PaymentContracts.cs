namespace TourKit.Api.Finance;

public sealed record CreatePaymentRequest(
    Guid? ProviderId, Guid? OrderCostId, decimal Amount, string PaymentMethod,
    string? Partner, string? ReceiverName, string? Note);

public sealed record PaymentResponse(
    Guid Id, string Code, Guid OrderId, Guid? ProviderId, Guid? OrderCostId,
    decimal Amount, string PaymentMethod, DateTimeOffset IssuedAt,
    string? Partner, string? ReceiverName, string? Note, int Status, bool IsRecognized);
