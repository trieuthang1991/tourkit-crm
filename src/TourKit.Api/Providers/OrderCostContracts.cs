namespace TourKit.Api.Providers;

public sealed record CreateOrderCostRequest(
    Guid ProviderId, string? ServiceName, int DayIndex, decimal ExpectedAmount, decimal ActualAmount,
    decimal Deposit, decimal Surcharge, decimal Vat, int Status);

public sealed record OrderCostResponse(
    Guid Id, Guid OrderId, Guid ProviderId, string? ServiceName, int DayIndex,
    decimal ExpectedAmount, decimal ActualAmount, decimal Deposit, decimal Surcharge, decimal Vat, int Status);
