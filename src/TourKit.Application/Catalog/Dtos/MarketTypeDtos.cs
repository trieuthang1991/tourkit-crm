namespace TourKit.Application.Catalog.Dtos;

public sealed record MarketTypeDto(Guid Id, string Name, Guid? ParentId, int SortOrder, int Status);

public sealed record CreateMarketTypeDto(string Name, Guid? ParentId, int SortOrder);

public sealed record UpdateMarketTypeDto(string Name, Guid? ParentId, int SortOrder);
