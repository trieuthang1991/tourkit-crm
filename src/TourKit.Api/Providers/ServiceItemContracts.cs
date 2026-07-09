namespace TourKit.Api.Providers;

public sealed record CreateServiceItemRequest(string Code, string Name, int Category, int Status);

public sealed record UpdateServiceItemRequest(string Name, int Category, int Status);

public sealed record ServiceItemResponse(Guid Id, string Code, string Name, int Category, int Status);
