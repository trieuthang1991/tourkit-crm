namespace TourKit.Application.Catalog.Dtos;

public sealed record PaymentAccountDto(
    Guid Id, string Name, string? BankName, string? AccountNumber, string? AccountHolder,
    string? Branch, string? TransferNote, bool IsDefault, int SortOrder, int Status);

public sealed record CreatePaymentAccountDto(
    string Name, string? BankName, string? AccountNumber, string? AccountHolder,
    string? Branch, string? TransferNote, bool IsDefault, int SortOrder);

public sealed record UpdatePaymentAccountDto(
    string Name, string? BankName, string? AccountNumber, string? AccountHolder,
    string? Branch, string? TransferNote, bool IsDefault, int SortOrder);
