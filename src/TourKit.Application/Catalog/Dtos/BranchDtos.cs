namespace TourKit.Application.Catalog.Dtos;

public sealed record BranchDto(Guid Id, string Name, string? Code, int SortOrder, int Status);

public sealed record CreateBranchDto(string Name, string? Code, int SortOrder);

public sealed record UpdateBranchDto(string Name, string? Code, int SortOrder);
