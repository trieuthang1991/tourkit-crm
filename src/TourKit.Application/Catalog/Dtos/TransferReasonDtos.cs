namespace TourKit.Application.Catalog.Dtos;

public sealed record TransferReasonDto(Guid Id, string Name, int SortOrder, int Status);

public sealed record CreateTransferReasonDto(string Name, int SortOrder);

public sealed record UpdateTransferReasonDto(string Name, int SortOrder);
