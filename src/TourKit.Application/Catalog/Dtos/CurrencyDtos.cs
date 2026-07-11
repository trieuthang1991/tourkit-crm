namespace TourKit.Application.Catalog.Dtos;

public sealed record CurrencyDto(Guid Id, string Code, string Name, decimal RateToVnd, int SortOrder, int Status);

public sealed record CreateCurrencyDto(string Code, string Name, decimal RateToVnd, int SortOrder);

public sealed record UpdateCurrencyDto(string Code, string Name, decimal RateToVnd, int SortOrder);
