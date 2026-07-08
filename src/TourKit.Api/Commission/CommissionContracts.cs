namespace TourKit.Api.Commission;

public sealed record OrderProfitResponse(decimal Revenue, decimal Cost, decimal Profit);

public sealed record CreateProfitShareRequest(Guid UserId, decimal Percentage);

public sealed record ProfitShareResponse(
    Guid Id, Guid OrderId, Guid UserId, decimal Percentage, decimal Amount, decimal ProfitBase);
