using TourKit.Shared.Enums;

namespace TourKit.Application.Providers.Dtos;

public sealed record ProviderDto(
    Guid Id, string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status);

public sealed record CreateProviderDto(
    string Code, string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status);

public sealed record UpdateProviderDto(
    string Name, ProviderType Type, string? Phone, string? Email, string? Address,
    string? TaxCode, string? ContactPerson, string? BankAccount, string? BankName, int Rate, int Status);
