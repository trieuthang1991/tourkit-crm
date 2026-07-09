namespace TourKit.Application.Providers.Dtos;

public sealed record ProviderServiceDto(
    Guid Id, Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status);

public sealed record CreateProviderServiceDto(
    Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status);

public sealed record UpdateProviderServiceDto(
    Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status);
