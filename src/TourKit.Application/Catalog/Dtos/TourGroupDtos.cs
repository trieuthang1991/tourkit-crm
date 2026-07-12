namespace TourKit.Application.Catalog.Dtos;

public sealed record TourGroupDto(Guid Id, string Name, string? Code, int SortOrder, int Status);

public sealed record CreateTourGroupDto(string Name, string? Code, int SortOrder);

public sealed record UpdateTourGroupDto(string Name, string? Code, int SortOrder);
