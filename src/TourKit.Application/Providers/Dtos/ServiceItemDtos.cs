namespace TourKit.Application.Providers.Dtos;

public sealed record ServiceItemDto(Guid Id, string Code, string Name, int Category, int Status);

public sealed record CreateServiceItemDto(string Code, string Name, int Category, int Status);

public sealed record UpdateServiceItemDto(string Name, int Category, int Status);
