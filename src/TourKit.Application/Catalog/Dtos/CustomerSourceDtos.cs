namespace TourKit.Application.Catalog.Dtos;

public sealed record CustomerSourceDto(Guid Id, string Name, int SortOrder, int Status);

public sealed record CreateCustomerSourceDto(string Name, int SortOrder);

public sealed record UpdateCustomerSourceDto(string Name, int SortOrder);
