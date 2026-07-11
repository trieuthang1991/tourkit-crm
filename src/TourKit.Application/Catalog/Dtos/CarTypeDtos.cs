namespace TourKit.Application.Catalog.Dtos;

public sealed record CarTypeDto(Guid Id, int Code, string Name, int SortOrder, int Status);

public sealed record CreateCarTypeDto(int Code, string Name, int SortOrder);

public sealed record UpdateCarTypeDto(int Code, string Name, int SortOrder);
