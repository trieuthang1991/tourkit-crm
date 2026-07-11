namespace TourKit.Application.Catalog.Dtos;

public sealed record LanguageTypeDto(Guid Id, string Name, string? Code, int SortOrder, int Status);

public sealed record CreateLanguageTypeDto(string Name, string? Code, int SortOrder);

public sealed record UpdateLanguageTypeDto(string Name, string? Code, int SortOrder);
