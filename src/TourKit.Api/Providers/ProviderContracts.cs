
using TourKit.Shared.Enums;

namespace TourKit.Api.Providers;

public sealed record CreateProviderRequest(
    string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status);

public sealed record UpdateProviderRequest(
    string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status);

public sealed record ProviderResponse(
    Guid Id, string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status);
