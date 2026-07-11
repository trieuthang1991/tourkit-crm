namespace TourKit.Application.Catalog.Dtos;

public sealed record DepartmentDto(Guid Id, string Name, string? Code, int SortOrder, int Status);

public sealed record CreateDepartmentDto(string Name, string? Code, int SortOrder);

public sealed record UpdateDepartmentDto(string Name, string? Code, int SortOrder);

public sealed record PositionDto(Guid Id, string Name, int SortOrder, int Status);

public sealed record CreatePositionDto(string Name, int SortOrder);

public sealed record UpdatePositionDto(string Name, int SortOrder);
