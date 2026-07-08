namespace TourKit.Api.Finance;

public sealed record CreateReceiptRequest(decimal Amount, string PaymentMethod, string? Partner, string? Note);

public sealed record ReceiptResponse(
    Guid Id, string Code, Guid OrderId, decimal Amount, string PaymentMethod,
    DateTimeOffset IssuedAt, string? Partner, string? Note, int Status, bool IsRecognized);

public sealed record OrderBalanceResponse(Guid OrderId, decimal Total, decimal Paid, decimal Outstanding);
