namespace TourKit.Application.Catalog.Dtos;

public sealed record CustomerTypeDto(Guid Id, int Code, string Name, int SortOrder, int Status);

public sealed record CreateCustomerTypeDto(int Code, string Name, int SortOrder);

public sealed record UpdateCustomerTypeDto(int Code, string Name, int SortOrder);
