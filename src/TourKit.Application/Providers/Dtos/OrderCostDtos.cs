namespace TourKit.Application.Providers.Dtos;

public sealed record OrderCostDto(
    Guid Id, Guid OrderId, Guid ProviderId, Guid? ProviderServiceId, string? ServiceName, int DayIndex,
    decimal ExpectedAmount, decimal ActualAmount, decimal Deposit, decimal Surcharge, decimal Vat, int Status);

public sealed record CreateOrderCostDto(
    Guid ProviderId, Guid? ProviderServiceId, string? ServiceName, int DayIndex, decimal ExpectedAmount,
    decimal ActualAmount, decimal Deposit, decimal Surcharge, decimal Vat, int Status);
