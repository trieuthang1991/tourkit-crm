namespace TourKit.Application.Operations;

public sealed record GuideTransactionDto(
    Guid Id, Guid TourGuideAssignmentId, int Type, decimal Amount, string Description, DateTimeOffset OccurredAt);

public sealed record CreateGuideTransactionDto(int Type, decimal Amount, string Description, DateTimeOffset? OccurredAt);

/// <summary>Đối soát thu-chi HDV: tổng thu, tổng chi, net (thu − chi) + các dòng.</summary>
public sealed record GuideSettlementDto(
    decimal TotalRevenue, decimal TotalExpense, decimal Net, GuideTransactionDto[] Items);
