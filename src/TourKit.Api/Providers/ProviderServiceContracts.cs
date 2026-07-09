namespace TourKit.Api.Providers;

public sealed record CreateProviderServiceRequest(
    Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status);

public sealed record UpdateProviderServiceRequest(
    Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status);

public sealed record ProviderServiceResponse(
    Guid Id, Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice,
    int AmountOfPeople, string? Note, int Status);
