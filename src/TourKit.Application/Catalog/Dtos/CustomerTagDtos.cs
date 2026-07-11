namespace TourKit.Application.Catalog.Dtos;

public sealed record CustomerTagDto(Guid Id, string Name, string? Color, int SortOrder, int Status);

public sealed record CreateCustomerTagDto(string Name, string? Color, int SortOrder);

public sealed record UpdateCustomerTagDto(string Name, string? Color, int SortOrder);
